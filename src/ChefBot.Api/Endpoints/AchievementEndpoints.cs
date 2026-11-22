using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The household's kitchen achievements, for the Plan and History tabs.</summary>
public static class AchievementEndpoints
{
    /// <summary>
    /// The first steps, badges, points and chef rank the household has earned so far, and in <c>unseen</c> what
    /// the signed-in person hasn't been shown yet.
    /// </summary>
    [WolverineGet("/api/achievements")]
    public static Task<AchievementsResponse> Get(HttpContext http, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetAchievementsAsync(Sessions.RequireUserId(http.User), cancellationToken);

    /// <summary>Says the signed-in person was shown the new achievements up to <c>upTo</c>, so they aren't shown again.</summary>
    [WolverinePost("/api/achievements/seen")]
    public static async Task<IResult> Seen(
        AchievementsSeenRequest request, HttpContext http, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken)
    {
        await recipes.MarkAchievementsSeenAsync(Sessions.RequireUserId(http.User), request, cancellationToken);
        return Results.NoContent();
    }
}
