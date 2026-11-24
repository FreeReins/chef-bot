using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>The plan's shopping list: what to buy, buying it, and where.</content>
internal sealed partial class PlanManager
{
    public async Task<IngredientsResponse> GetIngredientsAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var today = clock.Today();
        var expiringSoonDays = (await settings.GetAdvancedAsync(cancellationToken)).ExpiringSoonDays;
        var overview = shopping.BuildOverview(plan, await pantry.ListAsync(cancellationToken), today, expiringSoonDays);
        return overview.ToResponse(today, expiringSoonDays, catalog);
    }

    public async Task<PlanResponse> PurchaseAsync(Guid planId, Guid itemId, PurchaseDetails details, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        ValidationEngine.CanShopFor(plan);
        var item = ValidationEngine.ShoppingItem(plan, itemId);
        ValidationEngine.NotYetPurchased(item);

        await StockAsync(item, details, cancellationToken);
        await plans.SaveAsync(plan, cancellationToken);
        await messages.PublishAsync(new ShoppingItemBought(plan.Id, item.Id));
        await messages.PublishAsync(new PlanChanged(plan.Id));
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    public async Task<PlanResponse> PurchaseAllAsync(Guid planId, PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        ValidationEngine.CanShopFor(plan);
        var toBuy = plan.ShoppingItems.Where(item => !item.IsPurchased && AtStore(item.Store, request.Store)).ToList();
        foreach (var item in toBuy)
        {
            await StockAsync(item, new PurchaseDetails(request.PurchasedOn), cancellationToken);
        }

        var boughtByHand = await StockManualItemsAsync(request, cancellationToken);
        await AnnounceHaulAsync(plan.Id, toBuy.Count + boughtByHand, request);
        await messages.PublishAsync(new PlanChanged(plan.Id));
        await plans.SaveAsync(plan, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        if (boughtByHand > 0)
        {
            await ReconcileAfterBuyingAsync(plan, cancellationToken);
        }

        return plan.ToResponse([]);
    }

    public async Task<PlanResponse> SetShoppingStoreAsync(Guid planId, Guid itemId, ShoppingStoreRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var item = ValidationEngine.ShoppingItem(plan, itemId);
        ValidationEngine.NotYetPurchased(item);
        var stores = await settings.ListStoresAsync(cancellationToken);
        ShoppingEngine.SendTo(stores, item, ValidationEngine.StoreNamed(stores, request.Store));

        await settings.SaveStoresAsync(stores, cancellationToken);
        await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken, stores);
        await messages.PublishAsync(new GroceryStoresChanged()); // the other open plans follow in the background
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    /// <summary>Whether something bought at <paramref name="store"/> is wanted: everything when no store is asked for.</summary>
    private static bool AtStore(string store, string? wanted) =>
        wanted is null || string.Equals(store, wanted.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Ticks a plan's shopping item off and puts it in the pantry.</summary>
    private async Task StockAsync(ShoppingItem item, PurchaseDetails details, CancellationToken cancellationToken)
    {
        item.PurchasedOn = await AddToPantryAsync(item, item.Quantity, item.Unit, details, cancellationToken);
        item.IsPurchased = true;
    }

    /// <summary>
    /// After hand-added items were bought and committed, brings the plan being shown in line with the pantry they went
    /// into; the other open plans follow in the background (<see cref="PantryChanged"/>). A second commit, because the
    /// pantry is read from the database: what was bought counts once it is saved.
    /// </summary>
    private async Task ReconcileAfterBuyingAsync(MealPlan? changed, CancellationToken cancellationToken)
    {
        if (changed is not null)
        {
            await ReconcileAsync(changed, await SnapshotAsync(cancellationToken), cancellationToken);
        }

        await messages.PublishAsync(new PantryChanged());
        await messages.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Announces a shopping trip that bought everything left on the list, when it bought anything; the food history
    /// follows. A shop recorded on a later day keeps its day but no time.
    /// </summary>
    private async Task AnnounceHaulAsync(Guid? planId, int bought, PurchaseAllRequest request)
    {
        if (bought > 0)
        {
            var today = clock.Today();
            await messages.PublishAsync(new ShoppingDone(
                planId, bought, request.PurchasedOn ?? today, request.PurchasedOn is { } on && on != today ? null : clock.GetUtcNow()));
        }
    }

    /// <summary>Saves something bought to the pantry. Returns the day it was bought.</summary>
    private async Task<DateOnly> AddToPantryAsync(
        IIngredientRef ingredient, decimal? listed, string unit, PurchaseDetails details, CancellationToken cancellationToken)
    {
        var purchasedOn = details.PurchasedOn ?? clock.Today();
        var quantity = details.Quantity ?? listed;
        ValidationEngine.Stock(quantity, purchasedOn, details.ExpiresOn);

        await pantry.SaveAsync(transformation.ToPantryItem(ingredient, quantity, unit, purchasedOn, details.ExpiresOn), cancellationToken);
        return purchasedOn;
    }
}
