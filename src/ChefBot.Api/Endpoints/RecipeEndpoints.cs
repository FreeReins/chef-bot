using ChefBot.Api.Contracts;
using ChefBot.Api.Messaging;
using ChefBot.Core.Common;
using ChefBot.Core.Recipes;
using ChefBot.Core.Recipes.Files;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The recipe library kept in ChefBot's own database, and importing into it from a file or Mealie.</summary>
public static class RecipeEndpoints
{
    [WolverineGet("/api/recipes")]
    public static async Task<IReadOnlyList<RecipeResponse>> List([FromServices] RecipeService recipes, CancellationToken cancellationToken)
    {
        var library = await recipes.ListAsync(cancellationToken);
        return library.Select(recipe => recipe.ToResponse()).ToList();
    }

    /// <summary>Adds a recipe. Ingredients are free-text lines such as "1 lb chicken thighs".</summary>
    [WolverinePost("/api/recipes")]
    public static async Task<RecipeResponse> Create(
        RecipeDraft request, [FromServices] RecipeService recipes, CancellationToken cancellationToken) =>
        (await recipes.CreateAsync(request, cancellationToken)).ToResponse();

    /// <summary>Edits a recipe. An imported recipe becomes a local one, so imports leave the edit alone.</summary>
    [WolverinePut("/api/recipes/{id}")]
    public static async Task<RecipeResponse> Update(
        RecipeDraft request, Guid id, [FromServices] RecipeService recipes, CancellationToken cancellationToken) =>
        (await recipes.UpdateAsync(id, request, cancellationToken)).ToResponse();

    /// <summary>Archives a recipe so it is never planned again. Past plans keep showing it.</summary>
    [WolverineDelete("/api/recipes/{id}")]
    public static Task Archive(Guid id, [FromServices] RecipeService recipes, CancellationToken cancellationToken) =>
        recipes.ArchiveAsync(id, cancellationToken);

    /// <summary>The kinds of recipe file that can be imported.</summary>
    [WolverineGet("/api/recipes/import/formats")]
    public static IReadOnlyList<RecipeFileFormatResponse> FileFormats([FromServices] RecipeFileReader files) =>
        files.Formats.Select(format => new RecipeFileFormatResponse(format.Name, format.Extensions)).ToList();

    /// <summary>
    /// Adds the recipes in a file, read with the format its name ending belongs to. Recipes whose name is
    /// already in the library are left alone, so the same file can be imported again safely.
    /// </summary>
    [WolverinePost("/api/recipes/import/file")]
    public static Task<FileImportResult> ImportFile(
        ImportFileRequest request, [FromServices] RecipeService recipes, CancellationToken cancellationToken) =>
        recipes.ImportFileAsync(request.FileName ?? string.Empty, request.Content ?? string.Empty, cancellationToken);

    /// <summary>Starts a Mealie import in the background. Poll the status endpoint for the outcome.</summary>
    [WolverinePost("/api/recipes/import/mealie")]
    public static async Task<IResult> ImportFromMealie(
        IMessageBus bus,
        [FromServices] RecipeImportService imports,
        [FromServices] ImportStatusTracker status,
        [FromServices] TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (!await imports.IsConfiguredAsync(cancellationToken))
        {
            throw new RuleViolationException("Mealie isn't set up. An admin can enter its address and an API token on the Settings tab.");
        }

        var queued = new ImportStatus("Running", clock.GetUtcNow());
        status.Set(queued);
        await bus.PublishAsync(new ImportMealieRecipes());
        return Results.Accepted("/api/recipes/import/status", queued);
    }

    [WolverineGet("/api/recipes/import/status")]
    public static ImportStatus GetImportStatus([FromServices] ImportStatusTracker status) => status.Current;
}
