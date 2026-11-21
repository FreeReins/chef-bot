using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>The web, as far as recipe pages go: addresses it knows give their HTML, any other fails.</summary>
internal sealed class FakeWebPages : IWebPageAccess
{
    public Dictionary<string, string> Pages { get; } = [];

    public List<Uri> Requested { get; } = [];

    public Task<WebPage> FetchAsync(Uri address, CancellationToken cancellationToken)
    {
        Requested.Add(address);
        return Pages.TryGetValue(address.AbsoluteUri, out var html)
            ? Task.FromResult(new WebPage(address, html))
            : throw new RecipeSourceException($"Nothing at {address}.");
    }
}
