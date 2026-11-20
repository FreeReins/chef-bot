using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Staging, confirming and reading weekly plans, changing their meals, and recording purchases.</summary>
public static class PlanEndpoints
{
    /// <summary>Proposes a plan for a week and saves it as staged, replacing an earlier staged plan.</summary>
    [WolverinePost("/api/plans/stage")]
    public static Task<PlanResponse> Stage(StagePlanRequest request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.StageAsync(request, cancellationToken);

    /// <summary>
    /// Rebuilds a plan around what is in the pantry, for example after shopping. Recipes that use food
    /// already bought are strongly preferred, and the new shopping list only holds what is still missing.
    /// </summary>
    [WolverinePost("/api/plans/{planId}/regenerate")]
    public static Task<PlanResponse> Regenerate(Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RegenerateFromPantryAsync(planId, cancellationToken);

    /// <summary>Clears a plan. The pantry is left alone, so food already bought stays on hand.</summary>
    [WolverineDelete("/api/plans/{planId}")]
    public static Task Clear(Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.ClearAsync(planId, cancellationToken);

    /// <summary>The plan for this week, or else the next upcoming one.</summary>
    [WolverineGet("/api/plans/current")]
    public static Task<PlanResponse> Current([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetCurrentAsync(cancellationToken);

    [WolverineGet("/api/plans/{planId}")]
    public static Task<PlanResponse> Get(Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetAsync(planId, cancellationToken);

    /// <summary>Makes a staged plan the working plan, which opens it up for shopping.</summary>
    [WolverinePost("/api/plans/{planId}/confirm")]
    public static Task<PlanResponse> Confirm(Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.ConfirmAsync(planId, cancellationToken);

    /// <summary>Recalculates leftovers and the shopping list, for example after editing the pantry.</summary>
    [WolverinePost("/api/plans/{planId}/refresh")]
    public static Task<PlanResponse> Refresh(Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshAsync(planId, cancellationToken);

    /// <summary>What the plan needs, where each ingredient comes from, and what the pantry holds.</summary>
    [WolverineGet("/api/plans/{planId}/ingredients")]
    public static Task<IngredientsResponse> Ingredients(Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetIngredientsAsync(planId, cancellationToken);

    /// <summary>
    /// Suggests replacements for a meal: recipes ranked by how much of the pantry they use,
    /// plus creative grain, protein and produce ideas for lunches and dinners.
    /// </summary>
    /// <param name="planId">The plan.</param>
    /// <param name="mealId">The meal to replace.</param>
    /// <param name="recipes">How many recipes to suggest. Defaults to 5.</param>
    /// <param name="creative">How many creative ideas to suggest. Defaults to 3.</param>
    /// <param name="plans">Injected.</param>
    /// <param name="cancellationToken">Injected.</param>
    [WolverineGet("/api/plans/{planId}/meals/{mealId}/alternatives")]
    public static Task<AlternativesResponse> Alternatives(
        Guid planId, Guid mealId, int? recipes, int? creative, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetAlternativesAsync(planId, mealId, recipes ?? 5, creative ?? 3, cancellationToken);

    /// <summary>Replaces a meal. Leftover lunches and the shopping list are updated to match.</summary>
    [WolverinePut("/api/plans/{planId}/meals/{mealId}")]
    public static Task<PlanResponse> ReplaceMeal(
        ReplaceMealRequest request, Guid planId, Guid mealId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.ReplaceMealAsync(planId, mealId, request, cancellationToken);

    /// <summary>Marks a meal as Planned, Cooked or Skipped. Cooking uses up pantry stock.</summary>
    [WolverinePost("/api/plans/{planId}/meals/{mealId}/status")]
    public static Task<PlanResponse> SetMealStatus(
        MealStatusRequest request, Guid planId, Guid mealId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.SetMealStatusAsync(planId, mealId, request, cancellationToken);

    /// <summary>Marks one item as bought. Send <c>{}</c> to accept today's date and the estimated expiration.</summary>
    [WolverinePost("/api/plans/{planId}/shopping/{itemId}/purchase")]
    public static Task<PlanResponse> Purchase(
        PurchaseDetails request, Guid planId, Guid itemId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.PurchaseAsync(planId, itemId, request, cancellationToken);

    /// <summary>Marks everything still on the list as bought.</summary>
    [WolverinePost("/api/plans/{planId}/shopping/purchase-all")]
    public static Task<PlanResponse> PurchaseAll(
        PurchaseAllRequest request, Guid planId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.PurchaseAllAsync(planId, request, cancellationToken);
}
