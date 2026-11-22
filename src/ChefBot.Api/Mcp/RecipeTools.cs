using System.ComponentModel;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace ChefBot.Api.Mcp;

/// <summary>MCP tools for the household's recipe library.</summary>
[McpServerToolType]
public sealed class RecipeTools(IRecipeManager recipes)
{
    [McpServerTool(Name = "search_recipes", ReadOnly = true, Idempotent = true)]
    [Description("Searches the household's recipe library. Each word of the query must appear in a recipe's name, ingredients or tags, and every filter given must match. An empty query lists every recipe. Each result names the people who have it as a favourite. Results come a page at a time.")]
    public Task<RecipePageResponse> SearchRecipes(
        [Description("Words to look for, such as \"chicken rice\". Leave empty for all recipes.")] string? query = null,
        [Description("Only recipes of this course.")] RecipeCourse? course = null,
        [Description("The page wanted, from 1.")] int page = 1,
        [Description("Recipes per page, from 1 to 100.")] int pageSize = 20,
        [Description("Only recipes with every one of these tags.")] string[]? tags = null,
        [Description("True for only recipes that are at least one person's favourite.")] bool favouritesOnly = false,
        [Description("True for only slow cooker recipes.")] bool slowCookerOnly = false,
        [Description("Only recipes ready within this many minutes.")] int? readyWithinMinutes = null,
        [Description("Only recipes in the cookbook with this name, from list_cookbooks.")] string? cookbook = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await recipes.SearchAsync(
            new RecipeSearchRequest(
                query, course, page, pageSize, tags, favouritesOnly, null, slowCookerOnly, readyWithinMinutes,
                Cookbook: await CookbookIdAsync(cookbook, cancellationToken)),
            cancellationToken));

    [McpServerTool(Name = "list_cookbooks", ReadOnly = true, Idempotent = true)]
    [Description("The household's cookbooks: named groups of recipes, with how many recipes are in each. Empty when the household has none; recipes don't need to be in a cookbook.")]
    public Task<IReadOnlyList<CookbookResponse>> ListCookbooks(CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => recipes.ListCookbooksAsync(cancellationToken));

    [McpServerTool(Name = "get_recipe", ReadOnly = true, Idempotent = true)]
    [Description("One recipe in full: ingredients with amounts, steps, servings and tags.")]
    public Task<RecipeResponse> GetRecipe(
        [Description("The recipe's id, from search_recipes or a plan's meals.")] Guid recipeId,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => recipes.GetAsync(recipeId, cancellationToken));

    [McpServerTool(Name = "import_recipe_from_url", OpenWorld = true)]
    [Description("Adds the recipe on a public web page to the library, so it can be planned. Fails when the page is already in the library or has no recipe ChefBot can read.")]
    public Task<WebImportResponse> ImportRecipeFromUrl(
        [Description("The recipe page's address, such as https://example.com/recipes/soup.")] string url,
        [Description("A cookbook to put the recipe in, by name, from list_cookbooks. Leave empty for none.")] string? cookbook = null,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(async () => await recipes.ImportFromWebAsync(
            new WebRecipeRequest(url, await CookbookIdAsync(cookbook, cancellationToken)), cancellationToken));

    /// <summary>The id of the cookbook with this name, in any case; null when no name is given.</summary>
    /// <exception cref="NotFoundException">There is no cookbook with that name.</exception>
    private async Task<Guid?> CookbookIdAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var all = await recipes.ListCookbooksAsync(cancellationToken);
        return all.FirstOrDefault(cookbook => string.Equals(cookbook.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))?.Id
            ?? throw new NotFoundException(all.Count == 0
                ? $"There is no cookbook called {name.Trim()}; the household has no cookbooks. Leave the cookbook out."
                : $"There is no cookbook called {name.Trim()}. The cookbooks are: {string.Join(", ", all.Select(cookbook => cookbook.Name))}.");
    }
}
