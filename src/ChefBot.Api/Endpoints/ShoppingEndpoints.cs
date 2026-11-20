using ChefBot.Api.Contracts;
using ChefBot.Core.Shopping;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Recording what was bought. Purchases go into the pantry with an expiration date.</summary>
public static class ShoppingEndpoints
{
    /// <summary>Marks one item as bought. Send <c>{}</c> to accept today's date and the estimated expiration.</summary>
    [WolverinePost("/api/plans/{planId}/shopping/{itemId}/purchase")]
    public static async Task<PlanResponse> Purchase(
        PurchaseDetails request, Guid planId, Guid itemId, [FromServices] ShoppingService shopping, CancellationToken cancellationToken) =>
        (await shopping.PurchaseAsync(planId, itemId, request, cancellationToken)).ToResponse();

    /// <summary>Marks everything still on the list as bought.</summary>
    [WolverinePost("/api/plans/{planId}/shopping/purchase-all")]
    public static async Task<PlanResponse> PurchaseAll(
        PurchaseAllRequest request, Guid planId, [FromServices] ShoppingService shopping, CancellationToken cancellationToken) =>
        (await shopping.PurchaseAllAsync(planId, request.PurchasedOn, cancellationToken)).ToResponse();
}
