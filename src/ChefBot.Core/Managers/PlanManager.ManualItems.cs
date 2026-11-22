using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>Things added to the shopping list by hand.</content>
internal sealed partial class PlanManager
{
    public async Task<IReadOnlyList<ManualShoppingItemResponse>> ListManualItemsAsync(CancellationToken cancellationToken) =>
        DescribeManualItems(await shoppingList.ListAsync(cancellationToken), await settings.ListStoresAsync(cancellationToken));

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> AddManualItemAsync(NewShoppingItem request, CancellationToken cancellationToken)
    {
        ValidationEngine.ShoppingRequest(request);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var list = await shoppingList.ListAsync(cancellationToken);
        var item = ShoppingEngine.AddToList(list, catalog.Identify(request.Name), request.Quantity, Units.Normalize(request.Unit), clock.Today());

        await shoppingList.SaveAsync(item, cancellationToken);
        await messages.PublishAsync(new ManualShoppingChanged());
        await messages.CommitAsync(cancellationToken);
        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> RemoveManualItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var item = await validation.ManualShoppingItemAsync(itemId, cancellationToken);
        await shoppingList.RemoveAsync(item.Id, cancellationToken);
        await messages.PublishAsync(new ManualShoppingChanged());
        await messages.CommitAsync(cancellationToken);
        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseManualItemAsync(
        Guid itemId, PurchaseDetails details, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var item = await validation.ManualShoppingItemAsync(itemId, cancellationToken);
        await StockAsync(item, details, cancellationToken);
        await messages.PublishAsync(new ManualShoppingChanged());
        await messages.CommitAsync(cancellationToken);
        await ReconcileAfterBuyingAsync(changed: null, cancellationToken);
        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseAllManualItemsAsync(
        PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var bought = await StockManualItemsAsync(request, cancellationToken);
        await AnnounceHaulAsync(null, bought, request);
        await messages.PublishAsync(new ManualShoppingChanged());
        await messages.CommitAsync(cancellationToken);
        if (bought > 0)
        {
            await ReconcileAfterBuyingAsync(changed: null, cancellationToken);
        }

        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> SetManualItemStoreAsync(
        Guid itemId, ShoppingStoreRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var item = await validation.ManualShoppingItemAsync(itemId, cancellationToken);
        var stores = await settings.ListStoresAsync(cancellationToken);
        ShoppingEngine.SendTo(stores, item, ValidationEngine.StoreNamed(stores, request.Store));

        await settings.SaveStoresAsync(stores, cancellationToken);
        await messages.PublishAsync(new GroceryStoresChanged()); // the open plans follow in the background
        await messages.PublishAsync(new ManualShoppingChanged());
        await messages.CommitAsync(cancellationToken);
        return DescribeManualItems(await shoppingList.ListAsync(cancellationToken), stores);
    }

    private IReadOnlyList<ManualShoppingItemResponse> DescribeManualItems(IReadOnlyList<ManualShoppingItem> list, IReadOnlyList<GroceryStore> stores)
    {
        var month = clock.Today().Month;
        return list
            .Select(item => item.ToResponse(shopping.StoreFor(item, month, stores), catalog))
            .OrderBy(item => item.Store, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Takes a hand-added item off the list and puts it in the pantry.</summary>
    private async Task StockAsync(ManualShoppingItem item, PurchaseDetails details, CancellationToken cancellationToken)
    {
        await AddToPantryAsync(item, item.Quantity, item.Unit, details, cancellationToken);
        await shoppingList.RemoveAsync(item.Id, cancellationToken);
    }

    /// <summary>Buys everything added by hand on the same day. Returns how many things were bought.</summary>
    private async Task<int> StockManualItemsAsync(PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        var list = await shoppingList.ListAsync(cancellationToken);
        foreach (var item in list)
        {
            await StockAsync(item, new PurchaseDetails(request.PurchasedOn), cancellationToken);
        }

        return list.Count;
    }
}
