using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// Cookbooks: named groups of recipes. A recipe can be in any number of cookbooks, or none, and removing a cookbook
/// leaves its recipes in the library. Anyone signed in can make and change them, as with recipes.
/// </summary>
public static class CookbookEndpoints
{
    /// <summary>Every cookbook, by name, with how many recipes are in each. Empty until someone makes one.</summary>
    [WolverineGet("/api/cookbooks")]
    public static Task<IReadOnlyList<CookbookResponse>> List([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ListCookbooksAsync(cancellationToken);

    /// <summary>Makes a new, empty cookbook.</summary>
    [WolverinePost("/api/cookbooks")]
    public static Task<CookbookResponse> Create(CookbookDraft request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CreateCookbookAsync(request, cancellationToken);

    /// <summary>Renames a cookbook or changes its description.</summary>
    [WolverinePut("/api/cookbooks/{id}")]
    public static Task<CookbookResponse> Update(
        Guid id, CookbookDraft request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.UpdateCookbookAsync(id, request, cancellationToken);

    /// <summary>Removes a cookbook. Its recipes stay in the library.</summary>
    [WolverineDelete("/api/cookbooks/{id}")]
    public static async Task<IResult> Remove(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken)
    {
        await recipes.RemoveCookbookAsync(id, cancellationToken);
        return Results.NoContent();
    }

    /// <summary>Puts recipes in a cookbook. Ones already in it stay as they are.</summary>
    [WolverinePost("/api/cookbooks/{id}/recipes")]
    public static Task<CookbookResponse> AddRecipes(
        Guid id, CookbookRecipesRequest request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.AddToCookbookAsync(id, request, cancellationToken);

    /// <summary>Takes a recipe out of a cookbook; it stays in the library.</summary>
    [WolverineDelete("/api/cookbooks/{id}/recipes/{recipeId}")]
    public static Task<CookbookResponse> RemoveRecipe(
        Guid id, Guid recipeId, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RemoveFromCookbookAsync(id, recipeId, cancellationToken);

    /// <summary>
    /// A cookbook's recipes as one file to download, named after the cookbook: schema.org JSON-LD unless
    /// <c>format</c> asks for <c>Pdf</c>, <c>Markdown</c> or <c>Text</c>.
    /// </summary>
    [WolverineGet("/api/cookbooks/{id}/export")]
    public static async Task<IResult> Export(
        Guid id, RecipeExportFormat? format, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        RecipeEndpoints.Download(await recipes.ExportCookbookAsync(id, RecipeEndpoints.Format(format), cancellationToken));
}
