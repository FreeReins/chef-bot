using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>The web, as far as photos go: addresses it knows give their bytes, any other fails.</summary>
internal sealed class FakeImageSource : IImageSourceAccess
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public List<string> Requested { get; } = [];

    public Task<byte[]> DownloadAsync(string url, int maxBytes, CancellationToken cancellationToken)
    {
        Requested.Add(url);
        return Files.TryGetValue(url, out var data)
            ? Task.FromResult(data.Take(maxBytes + 1).ToArray())
            : throw new RecipeSourceException($"Nothing at {url}.");
    }
}
