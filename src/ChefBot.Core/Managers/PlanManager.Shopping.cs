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
        var overview = shopping.BuildOverview(plan, await pantry.ListAsync(cancellationToken), today, options.ExpiringSoonDays);
        return overview.ToResponse(today, options.ExpiringSoonDays, catalog);
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
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    public async Task<PlanResponse> PurchaseAllAsync(Guid planId, PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        ValidationEngine.CanShopFor(plan);
        var toBuy = plan.ShoppingItems.Where(item => !item.IsPurchased).ToList();
        foreach (var item in toBuy)
        {
            await StockAsync(item, new PurchaseDetails(request.PurchasedOn), cancellationToken);
        }

        var boughtByHand = await StockManualItemsAsync(request, cancellationToken);
        await RecordHaulAsync(toBuy.Count + boughtByHand, request, cancellationToken);
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
        await ReconcileOpenPlansAsync(stores, plan, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    /// <summary>Ticks a plan's shopping item off and puts it in the pantry.</summary>
    private async Task StockAsync(ShoppingItem item, PurchaseDetails details, CancellationToken cancellationToken)
    {
        item.PurchasedOn = await AddToPantryAsync(item, item.Quantity, item.Unit, details, cancellationToken);
        item.IsPurchased = true;
    }

    /// <summary>
    /// After hand-added items were bought and committed, brings open plans' lists in line with the pantry they went
    /// into. A second commit, because the pantry is read from the database: what was bought counts once it is saved.
    /// </summary>
    private async Task ReconcileAfterBuyingAsync(MealPlan? changed, CancellationToken cancellationToken)
    {
        await ReconcileOpenPlansAsync(await settings.ListStoresAsync(cancellationToken), changed, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    /// <summary>Notes a shopping trip that bought everything left on the list in the food history.</summary>
    private async Task RecordHaulAsync(int bought, PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        if (bought > 0)
        {
            await history.AddAsync(
                new FoodMoment
                {
                    OccurredOn = request.PurchasedOn ?? clock.Today(),
                    OccurredAt = request.PurchasedOn is { } on && on != clock.Today() ? null : clock.GetUtcNow(),
                    Kind = FoodMomentKind.Haul,
                    Count = bought,
                },
                cancellationToken);
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
