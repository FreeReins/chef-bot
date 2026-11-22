using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The household's kitchen achievements, for the Plan and History tabs.</summary>
public static class AchievementEndpoints
{
    /// <summary>The first steps, badges, points and chef rank the household has earned so far.</summary>
    [WolverineGet("/api/achievements")]
    public static Task<AchievementsResponse> Get([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetAchievementsAsync(cancellationToken);
}
