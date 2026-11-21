using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IPlanManager"/>
/// <remarks>
/// Every change ends in <see cref="ReconcileAsync"/>, so leftover lunches and the shopping list always follow the
/// meals and the pantry. Changes run one at a time (<see cref="PlanWriteLock"/>).
/// </remarks>
internal sealed class PlanManager(
    IPlanAccess plans,
    IRecipeAccess recipes,
    IPantryAccess pantry,
    IShoppingListAccess shoppingList,
    ISettingsAccess settings,
    IKnownStoreAccess knownStores,
    IKnownPlanStyleAccess knownStyles,
    IToDoListAccess toDoLists,
    IMessageAccess messages,
    PlanningEngine planning,
    ShoppingEngine shopping,
    SuggestionEngine suggestions,
    TransformationEngine transformation,
    ValidationEngine validation,
    IngredientCatalog catalog,
    PlannerOptions options,
    PlanWriteLock writeLock,
    TimeProvider clock) : IPlanManager
{
    public async Task<PlanResponse> StageAsync(StagePlanRequest request, CancellationToken cancellationToken)
    {
        var style = await StyleAsync(request.StyleId, cancellationToken);
        var week = request.WeekStart ?? WeekLayout.UpcomingWeekStart(clock.Today());
        return await StageAsync(week, request.Replace, favorPantry: false, "manual", style, request.Keep ?? [], cancellationToken);
    }

    public async Task<PlanResponse> RegenerateFromPantryAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var style = await StyleNamedAsync(plan.StyleName, cancellationToken);
        return await StageAsync(plan.WeekStart, replace: true, favorPantry: true, "regenerate", style, [], cancellationToken);
    }

    public async Task ClearAsync(Guid planId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        await plans.RemoveAsync(plan.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<PlanResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var plan = await plans.FindCurrentAsync(clock.Today(), cancellationToken)
            ?? throw new NotFoundException("There is no plan for this week or a later one yet. Stage one to get started.");
        return plan.ToResponse([]);
    }

    public async Task<PlanResponse> GetAsync(Guid planId, CancellationToken cancellationToken) =>
        (await validation.PlanAsync(planId, cancellationToken)).ToResponse([]);

    public async Task<IReadOnlyList<PlanSummaryResponse>> ListOpenAsync(CancellationToken cancellationToken) =>
        (await plans.ListOpenAsync(clock.Today(), cancellationToken)).Select(plan => plan.ToSummary()).ToList();

    public async Task<PlanResponse> ConfirmAsync(Guid planId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        plan.Status = PlanStatus.Confirmed;
        await plans.SaveAsync(plan, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    public async Task<PlanResponse> RefreshAsync(Guid planId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    public async Task RefreshOpenPlansAsync(CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        await ReconcileOpenPlansAsync(await settings.ListStoresAsync(cancellationToken), changed: null, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<IngredientsResponse> GetIngredientsAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var today = clock.Today();
        var overview = shopping.BuildOverview(plan, await pantry.ListAsync(cancellationToken), today, options.ExpiringSoonDays);
        return overview.ToResponse(today, options.ExpiringSoonDays, catalog);
    }

    public async Task<AlternativesResponse> GetAlternativesAsync(
        Guid planId, Guid mealId, int recipeLimit, int creativeCount, CancellationToken cancellationToken)
    {
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var meal = ValidationEngine.Meal(plan, mealId);
        var library = await recipes.ListActiveAsync(cancellationToken);
        var found = suggestions.ForMeal(plan, meal, library, await SnapshotAsync(cancellationToken), recipeLimit, creativeCount, Random.Shared);
        return found.ToResponse(meal);
    }

    public async Task<PlanResponse> ReplaceMealAsync(Guid planId, Guid mealId, ReplaceMealRequest request, CancellationToken cancellationToken)
    {
        ValidationEngine.ReplaceRequest(request);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var meal = ValidationEngine.Meal(plan, mealId);

        if (request.RecipeId is Guid recipeId)
        {
            var recipe = await validation.RecipeAsync(recipeId, cancellationToken);
            ValidationEngine.CanBePlanned(recipe);
            planning.UseRecipe(meal, recipe);
        }
        else
        {
            var ingredients = transformation.ToMealIngredients(request.Custom!.Ingredients);
            ValidationEngine.CustomMeal(request.Custom.Title, ingredients);
            planning.UseCustom(meal, request.Custom.Title.Trim(), ingredients);
        }

        await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    public async Task<PlanResponse> MoveMealAsync(Guid planId, Guid mealId, MoveMealRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var meal = ValidationEngine.Meal(plan, mealId);
        ValidationEngine.CanMove(plan, meal, request.Date);
        if (meal.Date == request.Date)
        {
            return plan.ToResponse([]);
        }

        var from = meal.Date!.Value;
        var swapped = PlanningEngine.Move(plan, meal, request.Date);
        await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken);
        await messages.CommitAsync(cancellationToken);

        var note = swapped is null
            ? $"Moved {meal.Title} to {request.Date.DayOfWeek}."
            : $"Swapped {meal.Title} to {request.Date.DayOfWeek} and {swapped.Title} to {from.DayOfWeek}.";
        return plan.ToResponse([note]);
    }

    public async Task<PlanResponse> SetMealStatusAsync(Guid planId, Guid mealId, MealStatusRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        var meal = ValidationEngine.Meal(plan, mealId);
        if (meal.Status == request.Status)
        {
            return plan.ToResponse([]);
        }

        var items = await pantry.ListAsync(cancellationToken);
        if (request.Status == MealStatus.Cooked && meal.Status == MealStatus.Planned)
        {
            var finished = shopping.DeductCooked(items, meal);
            foreach (var item in items)
            {
                if (finished.Contains(item))
                {
                    await pantry.RemoveAsync(item.Id, cancellationToken);
                }
                else
                {
                    await pantry.SaveAsync(item, cancellationToken);
                }
            }

            items.RemoveAll(finished.Contains);
        }

        meal.Status = request.Status;
        var stock = new PantrySnapshot(items, clock.Today(), options.ExpiringSoonDays);
        await ReconcileAsync(plan, stock, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
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

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> ListManualItemsAsync(CancellationToken cancellationToken) =>
        DescribeManualItems(await shoppingList.ListAsync(cancellationToken), await settings.ListStoresAsync(cancellationToken));

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> AddManualItemAsync(NewShoppingItem request, CancellationToken cancellationToken)
    {
        ValidationEngine.ShoppingRequest(request);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var list = await shoppingList.ListAsync(cancellationToken);
        var item = ShoppingEngine.AddToList(list, catalog.Identify(request.Name), request.Quantity, Units.Normalize(request.Unit), clock.Today());

        await shoppingList.SaveAsync(item, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> RemoveManualItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var item = await validation.ManualShoppingItemAsync(itemId, cancellationToken);
        await shoppingList.RemoveAsync(item.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseManualItemAsync(
        Guid itemId, PurchaseDetails details, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var item = await validation.ManualShoppingItemAsync(itemId, cancellationToken);
        await StockAsync(item, details, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        await ReconcileAfterBuyingAsync(changed: null, cancellationToken);
        return await ListManualItemsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManualShoppingItemResponse>> PurchaseAllManualItemsAsync(
        PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var bought = await StockManualItemsAsync(request, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        if (bought)
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
        await ReconcileOpenPlansAsync(stores, changed: null, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return DescribeManualItems(await shoppingList.ListAsync(cancellationToken), stores);
    }

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
            var overview = shopping.BuildOverview(plan, await pantry.ListAsync(cancellationToken), today, options.ExpiringSoonDays);
            toBuy.AddRange(overview.Ingredients.Where(line => line.Status == IngredientStatus.ToBuy));
        }

        var stores = await settings.ListStoresAsync(cancellationToken);
        toBuy.AddRange((await shoppingList.ListAsync(cancellationToken)).Select(item => new OverviewLine(
            item.DisplayName, IngredientStatus.ToBuy, item.Quantity, item.Unit, shopping.StoreFor(item, today.Month, stores),
            catalog.Get(item.CatalogName)?.Group ?? IngredientGroup.Other, null, false, [], null)));

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

    public async Task<PlanResponse> PurchaseAllAsync(Guid planId, PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var plan = await validation.PlanAsync(planId, cancellationToken);
        ValidationEngine.CanShopFor(plan);
        foreach (var item in plan.ShoppingItems.Where(item => !item.IsPurchased))
        {
            await StockAsync(item, new PurchaseDetails(request.PurchasedOn), cancellationToken);
        }

        var boughtByHand = await StockManualItemsAsync(request, cancellationToken);
        await plans.SaveAsync(plan, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        if (boughtByHand)
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

    public async Task<GroceryStoresResponse> GetStoresAsync(CancellationToken cancellationToken) =>
        DescribeStores(await settings.ListStoresAsync(cancellationToken));

    public async Task<GroceryStoresResponse> SaveStoresAsync(SaveGroceryStoresRequest request, CancellationToken cancellationToken)
    {
        var details = ValidationEngine.GroceryStores(request.Stores);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var stores = details.ToStores(await settings.ListStoresAsync(cancellationToken));

        await settings.SaveStoresAsync(stores, cancellationToken);
        await ReconcileOpenPlansAsync(stores, changed: null, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return DescribeStores(stores);
    }

    public async Task<PlanStylesResponse> GetStylesAsync(CancellationToken cancellationToken) =>
        await DescribeStylesAsync(await settings.ListPlanStylesAsync(cancellationToken), cancellationToken);

    public async Task<PlanStylesResponse> SaveStylesAsync(SavePlanStylesRequest request, CancellationToken cancellationToken)
    {
        var details = ValidationEngine.PlanStyles(request.Styles);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var styles = details.ToStyles(await settings.ListPlanStylesAsync(cancellationToken));

        await settings.SavePlanStylesAsync(styles, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await DescribeStylesAsync(styles, cancellationToken);
    }

    public async Task CheckWeeklyScheduleAsync(CancellationToken cancellationToken)
    {
        var state = await settings.GetScheduleAsync(cancellationToken);
        if (planning.ScheduledWeekDue(clock.GetLocalNow(), state.LastStagedWeek) is not DateOnly week)
        {
            return;
        }

        // Recorded first, so a week that can't be planned (say, no recipes yet) isn't tried again every few minutes.
        state.LastStagedWeek = week;
        await settings.SaveScheduleAsync(state, cancellationToken);
        await messages.CommitAsync(cancellationToken);

        if (await plans.FindByWeekAsync(week, cancellationToken) is null)
        {
            var style = await StyleAsync(id: null, cancellationToken);
            await StageAsync(week, replace: false, favorPantry: false, "schedule", style, [], cancellationToken);
        }
    }

    public DateTimeOffset? NextScheduledStaging() => planning.NextScheduledStaging(clock.GetLocalNow());

    private async Task<PlanResponse> StageAsync(
        DateOnly weekStart, bool replace, bool favorPantry, string trigger, PlanStyle style, IReadOnlyList<Guid> keep,
        CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var existing = await validation.WeekToStageAsync(weekStart, replace, cancellationToken);
        var stock = await SnapshotAsync(cancellationToken);
        var kept = existing?.Meals.Where(meal => keep.Contains(meal.Id) && !meal.IsLeftover).ToList() ?? [];

        var draft = planning.Plan(new PlanRequest(
            weekStart,
            await recipes.ListActiveAsync(cancellationToken),
            await plans.RecipeIdsPlannedAsync(weekStart.AddDays(-options.AvoidRepeatDays), weekStart, cancellationToken),
            stock,
            clock.GetUtcNow(),
            // Staging the same week again should offer something different.
            Seed: existing is null ? null : Random.Shared.Next(),
            PantryBoost: favorPantry ? options.Weights.RegeneratePantryBoost : 1,
            Style: style,
            Kept: kept));

        if (existing is not null)
        {
            // Saved separately so the old and new plan never compete for the same week.
            await plans.RemoveAsync(existing.Id, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }

        await ReconcileAsync(draft.Plan, stock, cancellationToken);
        await messages.PublishAsync(new PlanStaged(draft.Plan.Id, trigger));
        await messages.CommitAsync(cancellationToken);

        return draft.Plan.ToResponse(draft.Warnings);
    }

    /// <summary>
    /// Brings a plan back in line after any change: leftover lunches follow the dinners, and the shopping list
    /// follows the meals, the pantry and the stores. Then saves it (the caller commits).
    /// </summary>
    /// <param name="plan">The plan to bring in line.</param>
    /// <param name="stock">The pantry.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <param name="stores">The stores to shop at. Read from the settings when not given.</param>
    private async Task ReconcileAsync(
        MealPlan plan, PantrySnapshot stock, CancellationToken cancellationToken, IReadOnlyList<GroceryStore>? stores = null)
    {
        stores ??= await settings.ListStoresAsync(cancellationToken);
        planning.PlaceLeftovers(plan);
        var list = shopping.BuildList(plan, stock, stores);
        shopping.ApplyList(plan, list);
        await plans.SaveAsync(plan, cancellationToken);
    }

    /// <summary>
    /// Reconciles every plan whose week hasn't ended with these stores, and <paramref name="changed"/> too when it is
    /// given, whatever its week.
    /// </summary>
    private async Task ReconcileOpenPlansAsync(
        IReadOnlyList<GroceryStore> stores, MealPlan? changed, CancellationToken cancellationToken)
    {
        var stock = await SnapshotAsync(cancellationToken);
        foreach (var plan in await plans.ListOpenAsync(clock.Today(), cancellationToken))
        {
            if (plan.Id != changed?.Id)
            {
                await ReconcileAsync(plan, stock, cancellationToken, stores);
            }
        }

        if (changed is not null)
        {
            await ReconcileAsync(changed, stock, cancellationToken, stores);
        }
    }

    /// <summary>The style with this id, or else the household's default: its default style, then the first starter style.</summary>
    private async Task<PlanStyle> StyleAsync(Guid? id, CancellationToken cancellationToken)
    {
        var saved = await settings.ListPlanStylesAsync(cancellationToken);
        return ValidationEngine.StyleWithId(saved, knownStyles.All(), id) ?? DefaultStyle(saved);
    }

    /// <summary>The saved style with this name, as when a plan is made again; the default style when it has gone.</summary>
    private async Task<PlanStyle> StyleNamedAsync(string name, CancellationToken cancellationToken)
    {
        var saved = await settings.ListPlanStylesAsync(cancellationToken);
        return saved.Concat(knownStyles.All()).FirstOrDefault(style => string.Equals(style.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? DefaultStyle(saved);
    }

    private PlanStyle DefaultStyle(IReadOnlyList<PlanStyle> saved) =>
        saved.FirstOrDefault(style => style.IsDefault) ?? saved.FirstOrDefault() ?? knownStyles.All().FirstOrDefault() ?? PlanStyle.Classic();

    private async Task<PlanStylesResponse> DescribeStylesAsync(IReadOnlyList<PlanStyle> styles, CancellationToken cancellationToken)
    {
        var tags = (await recipes.ListActiveAsync(cancellationToken))
            .SelectMany(recipe => recipe.Tags)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new PlanStylesResponse(
            styles.Select(style => style.ToResponse()).ToList(),
            knownStyles.All().Select(style => style.ToResponse(saved: false)).ToList(),
            tags,
            catalog.Entries.Where(entry => !entry.PantryStaple).Select(entry => entry.Name).Order(StringComparer.Ordinal).ToList());
    }

    private GroceryStoresResponse DescribeStores(IReadOnlyList<GroceryStore> stores) => new(
        stores.Select(store => store.ToResponse()).ToList(),
        knownStores.All().Select(store => store.ToResponse()).ToList(),
        catalog.Entries.Where(entry => !entry.PantryStaple).Select(entry => entry.Name).Order(StringComparer.Ordinal).ToList());

    private IReadOnlyList<ManualShoppingItemResponse> DescribeManualItems(IReadOnlyList<ManualShoppingItem> list, IReadOnlyList<GroceryStore> stores)
    {
        var month = clock.Today().Month;
        return list
            .Select(item => item.ToResponse(shopping.StoreFor(item, month, stores), catalog))
            .OrderBy(item => item.Store, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Ticks a plan's shopping item off and puts it in the pantry.</summary>
    private async Task StockAsync(ShoppingItem item, PurchaseDetails details, CancellationToken cancellationToken)
    {
        item.PurchasedOn = await AddToPantryAsync(item, item.Quantity, item.Unit, details, cancellationToken);
        item.IsPurchased = true;
    }

    /// <summary>Takes a hand-added item off the list and puts it in the pantry.</summary>
    private async Task StockAsync(ManualShoppingItem item, PurchaseDetails details, CancellationToken cancellationToken)
    {
        await AddToPantryAsync(item, item.Quantity, item.Unit, details, cancellationToken);
        await shoppingList.RemoveAsync(item.Id, cancellationToken);
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

    /// <summary>Buys everything added by hand on the same day. Returns false when there was nothing to buy.</summary>
    private async Task<bool> StockManualItemsAsync(PurchaseAllRequest request, CancellationToken cancellationToken)
    {
        var list = await shoppingList.ListAsync(cancellationToken);
        foreach (var item in list)
        {
            await StockAsync(item, new PurchaseDetails(request.PurchasedOn), cancellationToken);
        }

        return list.Count > 0;
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

    private async Task<PantrySnapshot> SnapshotAsync(CancellationToken cancellationToken) =>
        new(await pantry.ListAsync(cancellationToken), clock.Today(), options.ExpiringSoonDays);
}
