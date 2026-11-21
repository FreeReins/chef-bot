using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// The items put on the shopping list by hand. They need no plan, and buying one puts it in the pantry. Each
/// change returns the whole hand-added list.
/// </summary>
public static class ShoppingEndpoints
{
    /// <summary>What was put on the shopping list by hand, each with the store it is bought at.</summary>
    [WolverineGet("/api/shopping/items")]
    public static Task<IReadOnlyList<ManualShoppingItemResponse>> List([FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.ListManualItemsAsync(cancellationToken);

    /// <summary>Adds something to buy: <c>{"name": "milk", "quantity": 2, "unit": "l"}</c>. Amount and unit are optional.</summary>
    [WolverinePost("/api/shopping/items")]
    public static Task<IReadOnlyList<ManualShoppingItemResponse>> Add(
        NewShoppingItem request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.AddManualItemAsync(request, cancellationToken);

    /// <summary>Takes an item off the list without buying it.</summary>
    [WolverineDelete("/api/shopping/items/{itemId}")]
    public static Task<IReadOnlyList<ManualShoppingItemResponse>> Remove(Guid itemId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RemoveManualItemAsync(itemId, cancellationToken);

    /// <summary>Marks one item as bought and puts it in the pantry. Send <c>{}</c> for today and the estimated expiration.</summary>
    [WolverinePost("/api/shopping/items/{itemId}/purchase")]
    public static Task<IReadOnlyList<ManualShoppingItemResponse>> Purchase(
        PurchaseDetails request, Guid itemId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.PurchaseManualItemAsync(itemId, request, cancellationToken);

    /// <summary>Marks every hand-added item as bought.</summary>
    [WolverinePost("/api/shopping/items/purchase-all")]
    public static Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseAll(
        PurchaseAllRequest request, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.PurchaseAllManualItemsAsync(request, cancellationToken);

    /// <summary>
    /// Buys this item's ingredient at another store from now on: <c>{"store": "Costco"}</c>, or <c>{"store": ""}</c> for
    /// no particular store. Open plans' shopping lists follow.
    /// </summary>
    [WolverinePost("/api/shopping/items/{itemId}/store")]
    public static Task<IReadOnlyList<ManualShoppingItemResponse>> SetStore(
        ShoppingStoreRequest request, Guid itemId, [FromServices] IPlanManager plans, CancellationToken cancellationToken) =>
        plans.SetManualItemStoreAsync(itemId, request, cancellationToken);
}
