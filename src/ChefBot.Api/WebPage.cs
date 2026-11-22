using System.Text;
using Microsoft.AspNetCore.ResponseCompression;

namespace ChefBot.Api;

/// <summary>
/// Serves the web page built from src/ChefBot.Web on the same address as the API, so the sign-in cookie, the
/// Content-Security-Policy and offline mode all work without any cross-site setup.
/// </summary>
public static class WebPage
{
    private const string NotBuilt = "The web page hasn't been built yet. Run ./dev.sh web, then restart ChefBot.";

    private const string HelpNotBuilt =
        "The help pages aren't built into this copy of ChefBot. Read them in the docs folder of the repository, or at " +
        "https://freereins.github.io/chef-bot/.";

    /// <summary>
    /// Only the page's own files are compressed. API answers are left alone: compressing them over HTTPS next to
    /// the sign-in cookie would expose them to BREACH-style attacks.
    /// </summary>
    private static readonly string[] CompressedTypes =
    [
        "text/html", "text/css", "text/javascript", "application/javascript", "image/svg+xml", "application/manifest+json",
    ];

    /// <summary>
    /// Vite names the files under /assets after their content, so they can be cached for good; everything else,
    /// index.html above all, is checked on every visit, so an upgrade shows up straight away.
    /// </summary>
    private static readonly StaticFileOptions Files = new()
    {
        OnPrepareResponse = file => file.Context.Response.Headers.CacheControl =
            file.Context.Request.Path.StartsWithSegments("/assets") ? "public, max-age=31536000, immutable" : "no-cache",
    };

    /// <summary>
    /// The folder the page is served from: wwwroot in the app's folder in the published app, the container image
    /// and the Windows app, otherwise the build output in src/ChefBot.Web/dist, for dotnet run and dotnet watch.
    /// Found from the app's folder, not the current one, which for a Windows service is System32.
    /// </summary>
    public static string Folder() =>
        Path.Combine(AppContext.BaseDirectory, "wwwroot") is var published && Directory.Exists(published)
            ? published
            : Path.Combine("..", "ChefBot.Web", "dist");

    /// <summary>Adds Brotli and gzip compression for the page's files.</summary>
    public static IServiceCollection AddChefBotWebPage(this IServiceCollection services) =>
        services.AddResponseCompression(compression =>
        {
            compression.EnableForHttps = true;
            compression.Providers.Add<BrotliCompressionProvider>();
            compression.Providers.Add<GzipCompressionProvider>();
            compression.MimeTypes = CompressedTypes;
        });

    /// <summary>
    /// Serves the page's files, compressed, with index.html for "/", and the documentation under /help (see
    /// <see cref="UseHelp"/>).
    /// </summary>
    public static void UseChefBotWebPage(this WebApplication app)
    {
        app.UseResponseCompression();
        var helpNotFound = HelpNotFoundPage(app);
        app.Use(ForgetEndpointForHelp);
        app.UseDefaultFiles();
        app.UseStaticFiles(Files);
        UseHelp(app, helpNotFound);
    }

    /// <summary>
    /// Addresses such as /recipes/{id} or /settings/backups belong to the web page, so bookmarks and reloads work:
    /// any path that isn't a file gets index.html, which shows the matching view. Unknown /api paths still get a 404.
    /// When the page hasn't been built, every page address says so instead.
    /// </summary>
    public static void MapChefBotWebPage(this WebApplication app)
    {
        if (app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists)
        {
            app.MapFallbackToFile("index.html", Files).AllowAnonymous();
        }
        else
        {
            app.Logger.LogWarning("{Message} It was looked for in {Folder}.", NotBuilt, app.Environment.WebRootPath);
            app.MapFallback(() => Results.Text(NotBuilt, statusCode: StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();
        }

        app.MapFallback("/api/{**rest}", () => Results.NotFound()).AllowAnonymous().ExcludeFromDescription();
    }

    /// <summary>
    /// The documentation, built into the image under wwwroot/help (the Dockerfile's docs stage), is its own site: its
    /// pages are folders with an index.html, which the web page's fallback would otherwise answer with the app. Static
    /// files aren't served once an endpoint has matched, so the matched endpoint is dropped for /help before they run.
    /// </summary>
    private static Task ForgetEndpointForHelp(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/help"))
        {
            context.SetEndpoint(null);
        }

        return next(context);
    }

    /// <summary>
    /// Answers a /help address the static files didn't find. No sign-in is needed anywhere under /help, so the
    /// troubleshooting pages are there for someone who can't sign in. A missing page gets the site's own 404 page,
    /// never the web page; a copy without the docs built (dotnet run, ./dev.sh) says where to read them instead.
    /// </summary>
    private static void UseHelp(WebApplication app, string? notFound) =>
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/help"))
            {
                await next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            if (notFound is null)
            {
                await Results.Text(HelpNotBuilt, statusCode: StatusCodes.Status404NotFound).ExecuteAsync(context);
            }
            else
            {
                await Results.Content(notFound, "text/html; charset=utf-8", Encoding.UTF8, StatusCodes.Status404NotFound).ExecuteAsync(context);
            }
        });

    /// <summary>The documentation's 404 page, with its addresses moved under /help; null when the docs aren't built.</summary>
    private static string? HelpNotFoundPage(WebApplication app)
    {
        var file = app.Environment.WebRootFileProvider.GetFileInfo("help/404.html");
        if (!file.Exists || file.PhysicalPath is not { } path)
        {
            return null;
        }

        // MkDocs writes the 404 page's addresses from the site's root ("/assets/..."), which is /help/ here.
        return File.ReadAllText(path)
            .Replace("href=\"/", "href=\"/help/", StringComparison.Ordinal)
            .Replace("src=\"/", "src=\"/help/", StringComparison.Ordinal)
            .Replace("\"base\": \"/\"", "\"base\": \"/help/\"", StringComparison.Ordinal);
    }
}
