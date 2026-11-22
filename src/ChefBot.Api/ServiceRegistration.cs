using ChefBot.Api.Https;
using ChefBot.Api.Logging;
using ChefBot.Api.Messaging;
using ChefBot.Core;
using ChefBot.Core.Managers.Contracts.Services;
using JasperFx;
using JasperFx.CodeGeneration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Wolverine;

namespace ChefBot.Api;

/// <summary>The composition root: wires the core, sign-in, HTTPS and the background jobs together.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddChefBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddChefBotCore(configuration);
        services.AddHostedService<MaintenanceTimer>();
        return services;
    }

    /// <summary>
    /// Keeps the recent log entries in memory as well as writing them to the console, so admins can read them under
    /// Settings, Logs. The Logging:LogLevel filters apply to both.
    /// </summary>
    public static ILoggingBuilder AddChefBotLogViewer(this ILoggingBuilder logging)
    {
        logging.Services.AddSingleton<LogBuffer>();
        logging.Services.AddSingleton<ILoggerProvider, LogBufferProvider>();
        return logging;
    }

    /// <summary>
    /// Wolverine generates the code that calls endpoints and handlers. The managers are built by the container
    /// (they need the database context and HTTP clients), so Wolverine is told to ask the container for them.
    /// </summary>
    public static void ResolveChefBotServicesFromContainer(this WolverineOptions wolverine)
    {
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<IPlanManager>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<IPantryManager>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<IRecipeManager>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<IAccountManager>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<IMaintenanceManager>();
    }

    /// <summary>
    /// Where the code that calls endpoints and handlers comes from. Debug builds generate and compile it at startup
    /// (WolverineFx.RuntimeCompilation, Debug only), so a changed endpoint or handler needs no extra step. Release
    /// builds only load the code that <c>dotnet run -- codegen write</c> wrote into Internal/Generated before the
    /// build, which the container build does; they never compile while running, so they ship without Roslyn, and
    /// startup stops with an error naming any endpoint or handler whose code is missing.
    /// </summary>
    public static void UseChefBotGeneratedCode(this IServiceCollection services)
    {
#if DEBUG
        const TypeLoadMode mode = TypeLoadMode.Dynamic;
#else
        const TypeLoadMode mode = TypeLoadMode.Static;
#endif
        services.CritterStackDefaults(defaults =>
        {
            foreach (var profile in new[] { defaults.Development, defaults.Production })
            {
                profile.GeneratedCodeMode = mode;
                profile.AssertAllPreGeneratedTypesExist = mode == TypeLoadMode.Static;
            }
        });
    }

    /// <summary>
    /// Sign-in with a cookie, and with an API token for the MCP endpoint (<see cref="Sessions.McpPolicy"/>). The API answers 401 or 403 instead of redirecting to a login page, a session ends as
    /// soon as its account is removed or its password changes, and endpoints marked with the
    /// <see cref="Sessions.AdminPolicy"/> policy are for admins only.
    /// </summary>
    public static IServiceCollection AddChefBotSignIn(this IServiceCollection services, IConfiguration configuration)
    {
        // Cookies are encrypted with keys kept on disk, so sessions survive a restart.
        services.AddDataProtection().SetApplicationName("ChefBot").PersistKeysToFileSystem(new DirectoryInfo(KeysFolder(configuration)));

        services.AddAuthorizationBuilder()
            .AddPolicy(Sessions.AdminPolicy, policy => policy.RequireRole(Sessions.AdminRole))
            .AddPolicy(Sessions.McpPolicy, policy => policy.AddAuthenticationSchemes(ApiTokenAuthentication.SchemeName).RequireAuthenticatedUser());
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, AdminOnlyHandler>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthentication>(ApiTokenAuthentication.SchemeName, _ => { })
            .AddCookie(options =>
        {
            options.Cookie.Name = "chefbot.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
            options.Events.OnValidatePrincipal = async context =>
            {
                var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountManager>();
                var session = Sessions.UserId(context.Principal) is Guid userId
                    ? await accounts.FindSessionAsync(userId, context.HttpContext.RequestAborted)
                    : null;

                if (!Sessions.IsCurrent(context.Principal, session))
                {
                    context.RejectPrincipal();
                }
                else if (Sessions.AdminChanged(context.Principal, session!))
                {
                    context.ReplacePrincipal(Sessions.Principal(session!));
                    context.ShouldRenew = true;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Checks the data folders can be written, then loads the HTTPS certificate (creating a self-signed one on first
    /// start) and registers what serves HTTPS. This runs before the app is built because the web server needs the
    /// certificate to start listening, which also makes it the first place a badly mounted folder is noticed.
    /// </summary>
    public static CertificateStore AddChefBotHttps(this IServiceCollection services, IConfiguration configuration)
    {
        DataFolders.RequireWritable(KeysFolder(configuration), "Data:SessionKeys");
        DataFolders.RequireWritable(CoreRegistration.DataPath(configuration, "Data:Backups", "backups"), "Data:Backups");

        var options = configuration.GetSection("Https").Get<HttpsOptions>() ?? new HttpsOptions();
        var certificates = new CertificateStore(options, KeysFolder(configuration));
        certificates.Load();

        services.AddSingleton(certificates);
        services.AddSingleton<HttpsPolicy>();
        return certificates;
    }

    /// <summary>
    /// Behind a reverse proxy listed in Https:TrustedProxies, takes the visitor's address and whether they used HTTPS
    /// from the proxy's X-Forwarded-For and X-Forwarded-Proto headers. Off unless proxies are listed, because
    /// anyone could send those headers otherwise.
    /// </summary>
    /// <exception cref="InvalidOperationException">An entry in Https:TrustedProxies isn't an address or a network.</exception>
    public static void UseChefBotProxyHeaders(this WebApplication app)
    {
        var trusted = app.Services.GetRequiredService<CertificateStore>().Options.TrustedProxies
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (trusted.Length == 0)
        {
            return;
        }

        var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
        forwarded.KnownProxies.Clear();
        forwarded.KnownIPNetworks.Clear();
        foreach (var entry in trusted)
        {
            if (entry.Contains('/') && System.Net.IPNetwork.TryParse(entry, out var network))
            {
                forwarded.KnownIPNetworks.Add(network);
            }
            else if (System.Net.IPAddress.TryParse(entry, out var address))
            {
                forwarded.KnownProxies.Add(address);
            }
            else
            {
                throw new InvalidOperationException(
                    $"\"{entry}\" in Https:TrustedProxies isn't an IP address (10.0.0.5) or a network (172.16.0.0/12).");
            }
        }

        app.UseForwardedHeaders(forwarded);
    }

    /// <summary>
    /// The browser rules for ChefBot's own page: scripts, style sheets, images and API calls only from ChefBot
    /// itself, no plugins, and no other site showing ChefBot in a frame. Inline style attributes stay allowed,
    /// since the page's components use them for layout; inline scripts and style elements are refused.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; style-src-elem 'self'; " +
        "style-src-attr 'unsafe-inline'; img-src 'self'; font-src 'self'; connect-src 'self'; object-src 'none'; " +
        "base-uri 'none'; form-action 'self'; frame-ancestors 'none'";

    /// <summary>For the Swagger page at /docs, whose bundled script and styles ChefBot doesn't control: framing only.</summary>
    private const string DocsContentSecurityPolicy = "frame-ancestors 'none'; object-src 'none'";

    /// <summary>
    /// Adds the security headers to every response: the Content-Security-Policy above, no guessing at file types
    /// (so a stored photo is only ever shown as an image), no framing by other sites, and no addresses sent to
    /// other sites when a link is followed.
    /// </summary>
    public static void UseChefBotSecurityHeaders(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = context.Request.Path.StartsWithSegments("/docs") ? DocsContentSecurityPolicy : ContentSecurityPolicy;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY"; // for browsers that predate frame-ancestors
            headers["Referrer-Policy"] = "same-origin";
            await next(context);
        });
    }

    /// <summary>
    /// Sends plain HTTP requests to HTTPS when "always use HTTPS" is on, and with a certificate you provide, tells
    /// browsers to use HTTPS by themselves next time (HSTS).
    /// </summary>
    public static void UseChefBotHttpsRedirect(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var policy = context.RequestServices.GetRequiredService<HttpsPolicy>();
            if (policy.ShouldRedirect(context.Request))
            {
                // Temporary, so turning the setting off again takes effect in every browser.
                context.Response.Redirect(policy.HttpsUrl(context.Request), permanent: false, preserveMethod: true);
                return;
            }

            if (policy.ShouldSendHsts(context.Request))
            {
                context.Response.Headers.StrictTransportSecurity = HttpsPolicy.HstsHeader;
            }

            await next(context);
        });
    }

    /// <summary>
    /// Runs once at startup, before the app starts serving: creates the database tables (before the message bus
    /// creates its own, see <see cref="CoreRegistration.InitializeChefBotDatabaseAsync"/>) and loads the
    /// "always use HTTPS" setting.
    /// </summary>
    public static async Task PrepareChefBotAsync(this IServiceProvider services)
    {
        // Renewals and certificate problems found after startup are logged from here on.
        services.GetRequiredService<CertificateStore>().Logger =
            services.GetRequiredService<ILoggerFactory>().CreateLogger("ChefBot.Https");

        await services.InitializeChefBotDatabaseAsync();

        await using var scope = services.CreateAsyncScope();
        var rules = await scope.ServiceProvider.GetRequiredService<IAccountManager>().GetSecurityAsync(CancellationToken.None);
        services.GetRequiredService<HttpsPolicy>().RequireHttps = rules.RequireHttps;
    }

    /// <summary>
    /// Runs once the app and its message bus have started: saves the starting values from the configuration and
    /// the "always use HTTPS" default, writes a password reset link to the log when Accounts:RecoverUsername is set,
    /// and says when the weekly schedule next stages a plan.
    /// </summary>
    public static async Task ApplyChefBotStartupSettingsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var configuration = services.GetRequiredService<IConfiguration>();
        var loggers = services.GetRequiredService<ILoggerFactory>();

        // When ChefBot serves HTTPS, plain HTTP visitors are sent to it until an admin decides otherwise.
        var rules = await scope.ServiceProvider.GetRequiredService<IAccountManager>()
            .UseDefaultHttpsRuleAsync(services.GetRequiredService<CertificateStore>().IsEnabled, CancellationToken.None);
        services.GetRequiredService<HttpsPolicy>().RequireHttps = rules.RequireHttps;

        if (await scope.ServiceProvider.GetRequiredService<IPlanManager>().NextScheduledStagingAsync(CancellationToken.None) is { } next)
        {
            loggers.CreateLogger("ChefBot.Schedule").LogInformation("Next week's plan will be staged automatically at {When}.", next);
        }

        var username = configuration["Accounts:RecoverUsername"];
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var logger = loggers.CreateLogger("ChefBot.Recovery");
        var link = await scope.ServiceProvider.GetRequiredService<IAccountManager>().CreateRecoveryLinkAsync(username, CancellationToken.None);
        if (link is null)
        {
            logger.LogWarning("Accounts:RecoverUsername is set to {Username}, but there is no such account.", username);
        }
        else
        {
            logger.LogWarning(
                "Password reset for {Username}: open ChefBot and add /?reset={Token} to its address. " +
                "The link works once and expires {ExpiresAt:u}. Remove Accounts:RecoverUsername afterwards.",
                username, link.Token, link.ExpiresAt);
        }
    }

    /// <summary>The folder for files ChefBot creates and must keep: session keys and its own HTTPS certificate.</summary>
    private static string KeysFolder(IConfiguration configuration) => CoreRegistration.DataPath(configuration, "Data:SessionKeys", "keys");
}
