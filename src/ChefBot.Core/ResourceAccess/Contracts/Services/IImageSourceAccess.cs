namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Downloads recipe photos from the web, so ChefBot can keep its own copies.</summary>
internal interface IImageSourceAccess
{
    /// <summary>
    /// The file at an http or https address. Reading stops one byte past <paramref name="maxBytes"/>, so an
    /// oversized file is still recognised as too large without being read whole.
    /// </summary>
    /// <exception cref="Managers.Contracts.RecipeSourceException">The address can't be used, or the download failed.</exception>
    Task<byte[]> DownloadAsync(string url, int maxBytes, CancellationToken cancellationToken);
}
