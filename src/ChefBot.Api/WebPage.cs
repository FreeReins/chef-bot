using Microsoft.AspNetCore.ResponseCompression;

namespace ChefBot.Api;

/// <summary>
/// Serves the web page built from src/ChefBot.Web on the same address as the API, so the sign-in cookie, the
/// Content-Security-Policy and offline mode all work without any cross-site setup.
/// </summary>
public static class WebPage
{
    private const string NotBuilt = "The web page hasn't been built yet. Run ./dev.sh web, then restart ChefBot.";

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
    /// The folder the page is served from, relative to the app's folder: wwwroot in the published app and the
    /// container image, otherwise the build output in src/ChefBot.Web/dist, for dotnet run and dotnet watch.
    /// </summary>
    public static string Folder() =>
        Directory.Exists("wwwroot") ? "wwwroot" : Path.Combine("..", "ChefBot.Web", "dist");

    /// <summary>Adds Brotli and gzip compression for the page's files.</summary>
    public static IServiceCollection AddChefBotWebPage(this IServiceCollection services) =>
        services.AddResponseCompression(compression =>
        {
            compression.EnableForHttps = true;
            compression.Providers.Add<BrotliCompressionProvider>();
            compression.Providers.Add<GzipCompressionProvider>();
            compression.MimeTypes = CompressedTypes;
        });

    /// <summary>Serves the page's files, compressed, with index.html for "/".</summary>
    public static void UseChefBotWebPage(this WebApplication app)
    {
        app.UseResponseCompression();
        app.UseDefaultFiles();
        app.UseStaticFiles(Files);
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
}
