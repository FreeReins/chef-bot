using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Suggestions from an AI model for what recipes are missing, one recipe at a time or across the library.</summary>
public static class RecipeSuggestionEndpoints
{
    /// <summary>
    /// Asks the AI model chosen for completing recipes what this recipe is missing, and keeps the answer to look at.
    /// The recipe itself doesn't change. Answers 400 when no AI model is set up, and 502 when the model can't be
    /// reached or refuses.
    /// </summary>
    [WolverinePost("/api/recipes/{id}/suggestion")]
    public static Task<RecipeSuggestionResponse> Suggest(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.SuggestDetailsAsync(id, cancellationToken);

    /// <summary>The recipe's suggestions, with <c>stale</c> true when the recipe changed after they were made. Answers 404 when it has none.</summary>
    [WolverineGet("/api/recipes/{id}/suggestion")]
    public static Task<RecipeSuggestionResponse> Get(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetSuggestionAsync(id, cancellationToken);

    /// <summary>
    /// Makes the chosen changes, <c>{"changeIds": [1, 3]}</c>, to the recipe and returns it. Answers 409 when the recipe
    /// changed after the suggestions were made.
    /// </summary>
    [WolverinePost("/api/recipes/{id}/suggestion/apply")]
    public static Task<RecipeResponse> Apply(
        Guid id, ApplySuggestionRequest request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ApplySuggestionAsync(id, request, cancellationToken);

    /// <summary>Keeps the recipe as it is and sets the suggestions aside.</summary>
    [WolverineDelete("/api/recipes/{id}/suggestion")]
    public static Task Dismiss(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.DismissSuggestionAsync(id, cancellationToken);

    /// <summary>How many recipes are missing details, how many have suggestions waiting, and which model would look.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/ai/sweeps/recipe-details")]
    public static Task<DetailsSweepStatusResponse> SweepStatus([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetDetailsSweepAsync(cancellationToken);

    /// <summary>
    /// Queues every recipe missing details for the AI model to look at in the background. Each recipe is sent to the
    /// model, which may cost money. Answers 400 when no AI model is set up.
    /// </summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePost("/api/ai/sweeps/recipe-details")]
    public static Task<DetailsSweepResponse> StartSweep([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.StartDetailsSweepAsync(cancellationToken);
}
