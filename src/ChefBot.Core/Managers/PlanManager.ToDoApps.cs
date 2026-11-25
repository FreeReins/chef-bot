using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>Sending the shopping list to to-do apps.</content>
internal sealed partial class PlanManager
{
    public async Task<IReadOnlyList<ShoppingTargetResponse>> GetShoppingTargetsAsync(CancellationToken cancellationToken) =>
        (await settings.ListShoppingTargetsAsync(cancellationToken)).Select(target => target.ToResponse()).ToList();

    public async Task<IReadOnlyList<ShoppingTargetResponse>> SaveShoppingTargetsAsync(
        SaveShoppingTargetsRequest request, CancellationToken cancellationToken)
    {
        var current = await settings.ListShoppingTargetsAsync(cancellationToken);
        var targets = ValidationEngine.ShoppingTargets(request.Targets, current).ToShoppingTargets(current);

        await settings.SaveShoppingTargetsAsync(targets, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return targets.Select(target => target.ToResponse()).ToList();
    }

    public async Task<SendShoppingListResult> SendShoppingListAsync(SendShoppingListRequest request, CancellationToken cancellationToken)
    {
        var target = ValidationEngine.ShoppingTarget(await settings.ListShoppingTargetsAsync(cancellationToken), request.TargetId);
        var today = clock.Today();
        var toBuy = new List<OverviewLine>();
        if (request.PlanId is Guid planId)
        {
            var plan = await validation.PlanAsync(planId, cancellationToken);
            var overview = shopping.BuildOverview(plan, await pantry.ListAsync(cancellationToken), today, (await settings.GetAdvancedAsync(cancellationToken)).ExpiringSoonDays);
            toBuy.AddRange(overview.Ingredients.Where(line => line.Status == IngredientStatus.ToBuy));
        }

        var stores = await settings.ListStoresAsync(cancellationToken);
        toBuy.AddRange((await shoppingList.ListAsync(cancellationToken)).Select(item => new OverviewLine(
            item.DisplayName, IngredientStatus.ToBuy, item.Quantity, item.Unit, shopping.StoreFor(item, today.Month, stores),
            catalog.Get(item.CatalogName)?.Group ?? IngredientGroup.Other, null, false, [], null,
            catalog.Get(item.CatalogName)?.Area ?? StoreArea.Other, item.ExtraAmounts, item.OrInstead())));

        var items = ShoppingEngine.ToDoItems(toBuy);
        if (items.Count == 0)
        {
            throw new RuleViolationException("Nothing is left to buy, so nothing was sent.");
        }

        var delivery = await toDoLists.SendAsync(target, items, cancellationToken);
        return new SendShoppingListResult(delivery.Sent, items.Count, delivery.Problem);
    }

    public async Task<SendShoppingListResult> SendTestShoppingItemAsync(Guid targetId, CancellationToken cancellationToken)
    {
        var target = ValidationEngine.ShoppingTarget(await settings.ListShoppingTargetsAsync(cancellationToken), targetId);
        var delivery = await toDoLists.SendAsync(
            target, [new ToDoItem("Test item from ChefBot", "Sent to check the connection. You can delete it.")], cancellationToken);
        return new SendShoppingListResult(delivery.Sent, 1, delivery.Problem);
    }
}
