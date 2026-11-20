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
    ISettingsAccess settings,
    IKnownStoreAccess knownStores,
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
    public Task<PlanResponse> StageAsync(StagePlanRequest request, CancellationToken cancellationToken) =>
        StageAsync(request.WeekStart ?? WeekLayout.UpcomingWeekStart(clock.Today()), request.Replace, favorPantry: false, "manual", cancellationToken);

    public async Task<PlanResponse> RegenerateFromPantryAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await validation.PlanAsync(planId, cancellationToken);
        return await StageAsync(plan.WeekStart, replace: true, favorPantry: true, "regenerate", cancellationToken);
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
        var notes = await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse(notes);
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

        var notes = await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse(notes);
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
        var notes = await ReconcileAsync(plan, stock, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse(notes);
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
        foreach (var item in plan.ShoppingItems.Where(item => !item.IsPurchased))
        {
            await StockAsync(item, new PurchaseDetails(request.PurchasedOn), cancellationToken);
        }

        await plans.SaveAsync(plan, cancellationToken);
        await messages.CommitAsync(cancellationToken);
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
        var notes = await ReconcileOpenPlansAsync(stores, plan, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse(notes);
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

        if (options.ImportFromMealieBeforePlanning && (await settings.GetMealieAsync(cancellationToken)).IsConfigured)
        {
            await messages.PublishAsync(new ImportMealieRecipes(StageWeekAfter: week));
            await messages.CommitAsync(cancellationToken);
            return;
        }

        await messages.CommitAsync(cancellationToken);
        await StageScheduledWeekAsync(new StageScheduledWeek(week), cancellationToken);
    }

    public async Task StageScheduledWeekAsync(StageScheduledWeek command, CancellationToken cancellationToken)
    {
        if (await plans.FindByWeekAsync(command.WeekStart, cancellationToken) is null)
        {
            await StageAsync(command.WeekStart, replace: false, favorPantry: false, "schedule", cancellationToken);
        }
    }

    public DateTimeOffset? NextScheduledStaging() => planning.NextScheduledStaging(clock.GetLocalNow());

    private async Task<PlanResponse> StageAsync(DateOnly weekStart, bool replace, bool favorPantry, string trigger, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var existing = await validation.WeekToStageAsync(weekStart, replace, cancellationToken);
        var stock = await SnapshotAsync(cancellationToken);

        var draft = planning.Plan(new PlanRequest(
            weekStart,
            await recipes.ListActiveAsync(cancellationToken),
            await plans.RecipeIdsPlannedAsync(weekStart.AddDays(-options.AvoidRepeatDays), weekStart, cancellationToken),
            stock,
            clock.GetUtcNow(),
            // Staging the same week again should offer something different.
            Seed: existing is null ? null : Random.Shared.Next(),
            PantryBoost: favorPantry ? options.Weights.RegeneratePantryBoost : 1));

        if (existing is not null)
        {
            // Saved separately so the old and new plan never compete for the same week.
            await plans.RemoveAsync(existing.Id, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }

        var notes = await ReconcileAsync(draft.Plan, stock, cancellationToken);
        await messages.PublishAsync(new PlanStaged(draft.Plan.Id, trigger));
        await messages.CommitAsync(cancellationToken);

        return draft.Plan.ToResponse([.. draft.Warnings, .. notes]);
    }

    /// <summary>
    /// Brings a plan back in line after any change: leftover lunches follow the dinners, and the shopping list
    /// follows the meals, the pantry and the stores. Then saves it (the caller commits).
    /// </summary>
    /// <param name="plan">The plan to bring in line.</param>
    /// <param name="stock">The pantry.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <param name="stores">The stores to shop at. Read from the settings when not given.</param>
    /// <returns>Notes on what was left off the shopping list.</returns>
    private async Task<IReadOnlyList<string>> ReconcileAsync(
        MealPlan plan, PantrySnapshot stock, CancellationToken cancellationToken, IReadOnlyList<GroceryStore>? stores = null)
    {
        stores ??= await settings.ListStoresAsync(cancellationToken);
        planning.PlaceLeftovers(plan);
        var list = shopping.BuildList(plan, stock, stores);
        shopping.ApplyList(plan, list);
        await plans.SaveAsync(plan, cancellationToken);
        return list.Notes().ToList();
    }

    /// <summary>
    /// Reconciles every plan whose week hasn't ended with these stores, and <paramref name="changed"/> too when it is
    /// given, whatever its week.
    /// </summary>
    /// <returns>The notes for <paramref name="changed"/>, or none.</returns>
    private async Task<IReadOnlyList<string>> ReconcileOpenPlansAsync(
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

        return changed is null ? [] : await ReconcileAsync(changed, stock, cancellationToken, stores);
    }

    private GroceryStoresResponse DescribeStores(IReadOnlyList<GroceryStore> stores) => new(
        stores.Select(store => store.ToResponse()).ToList(),
        knownStores.All().Select(store => store.ToResponse()).ToList(),
        catalog.Entries.Where(entry => !entry.PantryStaple).Select(entry => entry.Name).Order(StringComparer.Ordinal).ToList());

    private async Task StockAsync(ShoppingItem item, PurchaseDetails details, CancellationToken cancellationToken)
    {
        var purchasedOn = details.PurchasedOn ?? clock.Today();
        var quantity = details.Quantity ?? item.Quantity;
        ValidationEngine.Stock(quantity, purchasedOn, details.ExpiresOn);

        await pantry.SaveAsync(transformation.ToPantryItem(item, quantity, item.Unit, purchasedOn, details.ExpiresOn), cancellationToken);
        item.IsPurchased = true;
        item.PurchasedOn = purchasedOn;
    }

    private async Task<PantrySnapshot> SnapshotAsync(CancellationToken cancellationToken) =>
        new(await pantry.ListAsync(cancellationToken), clock.Today(), options.ExpiringSoonDays);
}
