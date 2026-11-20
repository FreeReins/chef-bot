using ChefBot.Api.Contracts;
using ChefBot.Core.Common;
using ChefBot.Core.Swaps;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Changing individual meals on a plan.</summary>
public static class MealEndpoints
{
    /// <summary>
    /// Suggests replacements for a meal: recipes ranked by how much of the pantry they use,
    /// plus creative grain, protein and produce ideas for lunches and dinners.
    /// </summary>
    /// <param name="planId">The plan.</param>
    /// <param name="mealId">The meal to replace.</param>
    /// <param name="recipes">How many recipes to suggest. Defaults to 5.</param>
    /// <param name="creative">How many creative ideas to suggest. Defaults to 3.</param>
    /// <param name="meals">Injected.</param>
    /// <param name="cancellationToken">Injected.</param>
    [WolverineGet("/api/plans/{planId}/meals/{mealId}/alternatives")]
    public static async Task<AlternativesResponse> Alternatives(
        Guid planId, Guid mealId, int? recipes, int? creative, [FromServices] MealService meals, CancellationToken cancellationToken) =>
        (await meals.GetAlternativesAsync(planId, mealId, recipes ?? 5, creative ?? 3, cancellationToken)).ToResponse();

    /// <summary>Replaces a meal. Leftover lunches and the shopping list are updated to match.</summary>
    [WolverinePut("/api/plans/{planId}/meals/{mealId}")]
    public static async Task<PlanResponse> Replace(
        ReplaceMealRequest request, Guid planId, Guid mealId, [FromServices] MealService meals, CancellationToken cancellationToken)
    {
        var view = request switch
        {
            { RecipeId: Guid recipeId, Custom: null } =>
                await meals.UseRecipeAsync(planId, mealId, recipeId, cancellationToken),
            { RecipeId: null, Custom: { } custom } =>
                await meals.UseCustomAsync(planId, mealId, custom.Title, custom.Ingredients, cancellationToken),
            _ => throw new RuleViolationException("Send either recipeId or custom, but not both."),
        };

        return view.ToResponse();
    }

    /// <summary>Marks a meal as Planned, Cooked or Skipped. Cooking uses up pantry stock.</summary>
    [WolverinePost("/api/plans/{planId}/meals/{mealId}/status")]
    public static async Task<PlanResponse> SetStatus(
        MealStatusRequest request, Guid planId, Guid mealId, [FromServices] MealService meals, CancellationToken cancellationToken) =>
        (await meals.SetStatusAsync(planId, mealId, request.Status, cancellationToken)).ToResponse();
}
