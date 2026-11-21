using System.ComponentModel;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ModelContextProtocol.Server;

namespace ChefBot.Api.Mcp;

/// <summary>MCP tools for the household's recipe library.</summary>
[McpServerToolType]
public sealed class RecipeTools(IRecipeManager recipes)
{
    [McpServerTool(Name = "search_recipes", ReadOnly = true, Idempotent = true)]
    [Description("Searches the household's recipe library. Each word of the query must appear in a recipe's name, ingredients or tags. An empty query lists every recipe. Results come a page at a time.")]
    public Task<RecipePageResponse> SearchRecipes(
        [Description("Words to look for, such as \"chicken rice\". Leave empty for all recipes.")] string? query = null,
        [Description("Only recipes of this course.")] RecipeCourse? course = null,
        [Description("The page wanted, from 1.")] int page = 1,
        [Description("Recipes per page, from 1 to 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => recipes.SearchAsync(new RecipeSearchRequest(query, course, page, pageSize), cancellationToken));

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
        CancellationToken cancellationToken = default) =>
        McpErrors.Explain(() => recipes.ImportFromWebAsync(new WebRecipeRequest(url), cancellationToken));
}
