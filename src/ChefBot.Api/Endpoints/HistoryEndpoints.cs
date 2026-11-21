using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The household's food history, for the History tab.</summary>
public static class HistoryEndpoints
{
    /// <summary>
    /// Months of food history, newest first, such as <c>/api/history?before=2026-05-01&amp;months=6</c>. Both
    /// parameters are optional: no <c>before</c> starts from the newest month, and six months come back unless
    /// <c>months</c> asks for up to 24. Pass the response's <c>olderBefore</c> as <c>before</c> for the next ones.
    /// </summary>
    [WolverineGet("/api/history")]
    public static Task<HistoryResponse> Get(
        DateOnly? before, int? months, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetHistoryAsync(new HistoryRequest(before, months ?? 0), cancellationToken);
}
