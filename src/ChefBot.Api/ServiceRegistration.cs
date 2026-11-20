using ChefBot.Api.Https;
using ChefBot.Api.Messaging;
using ChefBot.Core.Accounts;
using ChefBot.Core.Backups;
using ChefBot.Core.Notifications;
using ChefBot.Core.Abstractions;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using ChefBot.Core.Recipes.Files;
using ChefBot.Core.Shopping;
using ChefBot.Core.Swaps;
using ChefBot.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Wolverine;

namespace ChefBot.Api;

/// <summary>The composition root: wires settings, the core logic and the infrastructure together.</summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddChefBot(this IServiceCollection services, IConfiguration configuration)
    {
        var planner = configuration.GetSection("Planner").Get<PlannerOptions>() ?? new PlannerOptions();
        planner.Validate();

        // Settings and reference data: loaded once, never changed while running.
        services.AddSingleton(planner);
        services.AddSingleton(configuration.GetSection("Classifier").Get<ClassifierOptions>() ?? new ClassifierOptions());
        services.AddSingleton(configuration.GetSection("Stores").Get<StoreOptions>() ?? new StoreOptions());
        services.AddSingleton(configuration.GetSection("Schedule").Get<WeeklyScheduleOptions>() ?? new WeeklyScheduleOptions());
        services.AddSingleton(IngredientCatalog.FromJson(File.ReadAllText(DataFile(configuration, "Data:IngredientCatalog", "config/ingredients.json"))));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ImportStatusTracker>();

        // Pure logic with no state of its own.
        services.AddSingleton<RecipeClassifier>();
        services.AddSingleton<RecipeIngredientBuilder>();
        services.AddSingleton<RecipeFileReader>();

        // Recipe file formats, chosen by file name ending and then by the first one that recognises the file.
        // Add a line here for each new kind of file; put specific formats before general ones like JsonRecipeFormat.
        services.AddSingleton<IRecipeFileFormat, MealieBackupFormat>();
        services.AddSingleton<IRecipeFileFormat, JsonRecipeFormat>();
        services.AddSingleton<WeekPlanner>();
        services.AddSingleton<StoreRouter>();
        services.AddSingleton<ShoppingListBuilder>();
        services.AddSingleton<PlanRefresher>();
        services.AddSingleton<IngredientOverviewBuilder>();
        services.AddSingleton<AlternativeFinder>();
        services.AddSingleton<CreativeMealGenerator>();
        services.AddSingleton<PantryItemFactory>();

        // Use cases: one instance per request or message, sharing one database transaction.
        services.AddScoped<RecipeService>();
        services.AddScoped<RecipeImportService>();
        services.AddScoped<PlanningService>();
        services.AddScoped<MealService>();
        services.AddScoped<ShoppingService>();
        services.AddScoped<PantryService>();
        services.AddScoped<AccountService>();
        services.AddScoped<ReminderService>();
        services.AddScoped<BackupService>();
        services.AddSingleton(new SampleRecipes(DataFile(configuration, "Data:SampleRecipes", "config/sample-recipes.json")));

        return services.AddChefBotInfrastructure(configuration);
    }

    /// <summary>
    /// Wolverine generates the code that calls endpoints and handlers. These services are built by
    /// the container (database context, HTTP clients), so Wolverine is told to ask the container for them.
    /// </summary>
    public static void ResolveChefBotServicesFromContainer(this WolverineOptions wolverine)
    {
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<RecipeService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<RecipeImportService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<PlanningService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<MealService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<ShoppingService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<PantryService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<AccountService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<ReminderService>();
        wolverine.CodeGeneration.AlwaysUseServiceLocationFor<BackupService>();
    }

    /// <summary>
    /// Sign-in with a cookie. The API answers 401 or 403 instead of redirecting to a login page,
    /// and a session ends as soon as its account is removed.
    /// </summary>
    public static IServiceCollection AddChefBotSignIn(this IServiceCollection services, IConfiguration configuration)
    {
        // Cookies are encrypted with keys kept on disk, so sessions survive a restart.
        services.AddDataProtection().SetApplicationName("ChefBot").PersistKeysToFileSystem(new DirectoryInfo(KeysFolder(configuration)));

        services.AddAuthorization();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
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
                var accounts = context.HttpContext.RequestServices.GetRequiredService<AccountService>();
                var user = Sessions.UserId(context.Principal) is Guid userId
                    ? await accounts.FindUserAsync(userId, context.HttpContext.RequestAborted)
                    : null;

                if (!Sessions.IsCurrent(context.Principal, user))
                {
                    context.RejectPrincipal();
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
        DataFolders.RequireWritable(DataFile(configuration, "Data:Backups", "backups"), "Data:Backups");

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

    /// <summary>Sends plain HTTP requests to HTTPS when an admin has turned "always use HTTPS" on.</summary>
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

            await next(context);
        });
    }

    /// <summary>
    /// Runs once at startup: creates the database tables, loads the admin's settings, and, when
    /// Accounts:RecoverUsername is set, writes a password reset link for that account to the log.
    /// </summary>
    public static async Task PrepareChefBotAsync(this IServiceProvider services)
    {
        // Renewals and certificate problems found after startup are logged from here on.
        services.GetRequiredService<CertificateStore>().Logger =
            services.GetRequiredService<ILoggerFactory>().CreateLogger("ChefBot.Https");

        await services.InitializeChefBotDatabaseAsync();

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        var rules = await accounts.GetSecuritySettingsAsync(CancellationToken.None);
        services.GetRequiredService<HttpsPolicy>().RequireHttps = rules.RequireHttps;

        // A webhook and a Mealie connection in the configuration are the starting values until something
        // is saved on the Settings tab.
        var configuration = services.GetRequiredService<IConfiguration>();
        await scope.ServiceProvider.GetRequiredService<ReminderService>()
            .UseDefaultWebhookAsync(configuration["Notifications:WebhookUrl"], CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<RecipeImportService>()
            .UseDefaultConnectionAsync(configuration["Mealie:Url"], configuration["Mealie:ApiToken"], CancellationToken.None);

        var username = configuration["Accounts:RecoverUsername"];
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ChefBot.Recovery");
        var link = await accounts.CreatePasswordResetForUsernameAsync(username, CancellationToken.None);
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
    private static string KeysFolder(IConfiguration configuration) => DataFile(configuration, "Data:SessionKeys", "keys");

    /// <summary>Resolves a data file path from settings. Relative paths are relative to the application folder.</summary>
    private static string DataFile(IConfiguration configuration, string key, string fallback)
    {
        var path = configuration[key] is { Length: > 0 } configured ? configured : fallback;
        return Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    }
}
