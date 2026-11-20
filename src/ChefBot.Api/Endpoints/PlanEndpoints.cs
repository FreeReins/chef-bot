using ChefBot.Api.Contracts;
using ChefBot.Api.Messaging;
using ChefBot.Core.Common;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Planning;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Staging, confirming and reading weekly plans.</summary>
public static class PlanEndpoints
{
    /// <summary>Proposes a plan for a week and saves it as staged, replacing an earlier staged plan.</summary>
    [WolverinePost("/api/plans/stage")]
    public static async Task<PlanResponse> Stage(
        StagePlanRequest request, IMessageBus bus, [FromServices] PlanningService planning, CancellationToken cancellationToken)
    {
        var view = await planning.StageAsync(request.WeekStart, request.Replace, cancellationToken);
        await bus.PublishAsync(new PlanStaged(view.Plan.Id, "manual"));
        return view.ToResponse();
    }

    /// <summary>
    /// Rebuilds a plan around what is in the pantry, for example after shopping. Recipes that use food
    /// already bought are strongly preferred, and the new shopping list only holds what is still missing.
    /// </summary>
    [WolverinePost("/api/plans/{planId}/regenerate")]
    public static async Task<PlanResponse> Regenerate(
        Guid planId, IMessageBus bus, [FromServices] PlanningService planning, CancellationToken cancellationToken)
    {
        var view = await planning.RegenerateFromPantryAsync(planId, cancellationToken);
        await bus.PublishAsync(new PlanStaged(view.Plan.Id, "regenerate"));
        return view.ToResponse();
    }

    /// <summary>Clears a plan. The pantry is left alone, so food already bought stays on hand.</summary>
    [WolverineDelete("/api/plans/{planId}")]
    public static Task Clear(Guid planId, [FromServices] PlanningService planning, CancellationToken cancellationToken) =>
        planning.ClearAsync(planId, cancellationToken);

    /// <summary>The plan for this week, or else the next upcoming one.</summary>
    [WolverineGet("/api/plans/current")]
    public static async Task<PlanResponse> Current([FromServices] PlanningService planning, CancellationToken cancellationToken) =>
        (await planning.GetCurrentAsync(cancellationToken)).ToResponse();

    [WolverineGet("/api/plans/{planId}")]
    public static async Task<PlanResponse> Get(Guid planId, [FromServices] PlanningService planning, CancellationToken cancellationToken) =>
        (await planning.GetAsync(planId, cancellationToken)).ToResponse();

    /// <summary>Makes a staged plan the working plan, which opens it up for shopping.</summary>
    [WolverinePost("/api/plans/{planId}/confirm")]
    public static async Task<PlanResponse> Confirm(Guid planId, [FromServices] PlanningService planning, CancellationToken cancellationToken) =>
        (await planning.ConfirmAsync(planId, cancellationToken)).ToResponse();

    /// <summary>Recalculates leftovers and the shopping list, for example after editing the pantry.</summary>
    [WolverinePost("/api/plans/{planId}/refresh")]
    public static async Task<PlanResponse> Refresh(Guid planId, [FromServices] PlanningService planning, CancellationToken cancellationToken) =>
        (await planning.RefreshAsync(planId, cancellationToken)).ToResponse();

    /// <summary>What the plan needs, where each ingredient comes from, and what the pantry holds.</summary>
    [WolverineGet("/api/plans/{planId}/ingredients")]
    public static async Task<IngredientsResponse> Ingredients(
        Guid planId,
        [FromServices] PlanningService planning,
        [FromServices] PlannerOptions options,
        [FromServices] IngredientCatalog catalog,
        [FromServices] TimeProvider clock,
        CancellationToken cancellationToken) =>
        (await planning.GetIngredientsAsync(planId, cancellationToken)).ToResponse(clock.Today(), options, catalog);
}
