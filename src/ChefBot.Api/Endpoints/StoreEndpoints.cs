using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The stores the household shops at, which the shopping list is split between.</summary>
public static class StoreEndpoints
{
    /// <summary>The household's stores, the known stores that can be added, and the ingredient names a store can list.</summary>
    [WolverineGet("/api/stores")]
    public static Task<GroceryStoresResponse> Get([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.GetStoresAsync(cancellationToken);

    /// <summary>Replaces the household's stores and rebuilds open shopping lists to match. Admins only.</summary>
    [WolverinePut("/api/stores")]
    [Authorize(Policy = Sessions.AdminPolicy)]
    public static Task<GroceryStoresResponse> Save(
        SaveGroceryStoresRequest request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.SaveStoresAsync(request, cancellationToken);
}
