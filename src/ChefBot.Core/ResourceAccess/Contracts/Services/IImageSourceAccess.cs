namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>
/// Downloads recipe photos from the web, so ChefBot can keep its own copies. Like <see cref="IWebPageAccess"/>, it
/// refuses addresses on private networks unless told the address is one an admin chose.
/// </summary>
internal interface IImageSourceAccess
{
    /// <summary>
    /// The file at an http or https address. Reading stops one byte past <paramref name="maxBytes"/>, so an
    /// oversized file is still recognised as too large without being read whole.
    /// </summary>
    /// <param name="url">The photo's address.</param>
    /// <param name="maxBytes">The largest photo wanted.</param>
    /// <param name="allowPrivateAddresses">
    /// True when an admin chose the server, such as the configured Mealie, which is often on the home network.
    /// </param>
    /// <param name="cancellationToken">Stops the download.</param>
    /// <exception cref="Managers.Contracts.RecipeSourceException">The address can't be used, or the download failed.</exception>
    Task<byte[]> DownloadAsync(string url, int maxBytes, bool allowPrivateAddresses, CancellationToken cancellationToken);
}
