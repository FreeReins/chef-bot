using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>The web, as far as photos go: addresses it knows give their bytes, any other fails.</summary>
internal sealed class FakeImageSource : IImageSourceAccess
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public List<string> Requested { get; } = [];

    /// <summary>The addresses that were allowed to be on a private network.</summary>
    public List<string> PrivateAllowed { get; } = [];

    public Task<byte[]> DownloadAsync(string url, int maxBytes, bool allowPrivateAddresses, CancellationToken cancellationToken)
    {
        Requested.Add(url);
        if (allowPrivateAddresses) PrivateAllowed.Add(url);
        return Files.TryGetValue(url, out var data)
            ? Task.FromResult(data.Take(maxBytes + 1).ToArray())
            : throw new RecipeSourceException($"Nothing at {url}.");
    }
}
