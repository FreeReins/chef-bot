using AngleSharp.Dom;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>
/// A site reader shaped like a real one: for fussy.example it fetches the print version, starts from the page's
/// schema.org data and takes the ingredients from the HTML, where that site writes them properly.
/// </summary>
internal sealed class FakeSitePage(SchemaRecipe schema) : IRecipePageReader
{
    public string Name => "Fussy Example";

    public bool Handles(Uri url) => url.Host.EndsWith("fussy.example", StringComparison.OrdinalIgnoreCase);

    public Uri PageToFetch(Uri url) => new(url, url.AbsolutePath.TrimEnd('/') + "/print");

    public RecipeText? Read(Uri url, IDocument page) =>
        schema.FromPage(page) is { } recipe
            ? recipe with { Ingredients = page.QuerySelectorAll("li.ingredient").Select(item => item.TextContent.Trim()).ToList() }
            : null;
}
