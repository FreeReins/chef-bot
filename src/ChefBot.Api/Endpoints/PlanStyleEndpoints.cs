using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The plan styles weeks are planned in: dinner days, how many of each other meal, and rules about recipes.</summary>
public static class PlanStyleEndpoints
{
    /// <summary>The household's plan styles, the starter styles that can be added, and the tags and ingredients rules can use.</summary>
    [WolverineGet("/api/plan-styles")]
    public static Task<PlanStylesResponse> Get([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetStylesAsync(cancellationToken);

    /// <summary>Replaces the household's plan styles. Anyone signed in can change them.</summary>
    [WolverinePut("/api/plan-styles")]
    public static Task<PlanStylesResponse> Save(
        SavePlanStylesRequest request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.SaveStylesAsync(request, cancellationToken);

    /// <summary>How many people plans are for, when next week is planned by itself, and how long leftovers keep.</summary>
    [WolverineGet("/api/planning")]
    public static Task<PlannerSettingsResponse> GetPlanning([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetPlannerSettingsAsync(cancellationToken);

    /// <summary>
    /// Changes how many people plans are for, the weekly schedule and how long leftovers keep. Weeks not yet confirmed
    /// are resized. Anyone signed in can change them.
    /// </summary>
    [WolverinePut("/api/planning")]
    public static Task<PlannerSettingsResponse> SavePlanning(
        PlannerSettingsRequest request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.SavePlannerSettingsAsync(request, cancellationToken);
}
