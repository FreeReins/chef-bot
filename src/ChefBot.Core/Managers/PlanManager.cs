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
/// meals and the pantry. Adding a recipe leaves that to <see cref="CatchUpAsync"/>, run in the background. Changes run one at a time (<see cref="PlanWriteLock"/>).
/// </remarks>
internal sealed partial class PlanManager(
    IPlanAccess plans,
    IRecipeAccess recipes,
    IPantryAccess pantry,
    IShoppingListAccess shoppingList,
    ISettingsAccess settings,
    IKnownStoreAccess knownStores,
    IKnownPlanStyleAccess knownStyles,
    IToDoListAccess toDoLists,
    IFavouriteAccess favourites,
    ICookbookAccess cookbooks,
    IMessageAccess messages,
    PlanningEngine planning,
    ShoppingEngine shopping,
    SuggestionEngine suggestions,
    CalendarEngine calendar,
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
        await messages.PublishAsync(new PlanCleared(plan.Id));
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
        await messages.PublishAsync(new PlanConfirmed(plan.Id));
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
            PlanningEngine.UseRecipe(meal, recipe, plan.People);
        }
        else
        {
            var ingredients = transformation.ToMealIngredients(request.Custom!.Ingredients);
            ValidationEngine.CustomMeal(request.Custom.Title, ingredients);
            PlanningEngine.UseCustom(meal, request.Custom.Title.Trim(), ingredients, plan.People);
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

        await AnnounceCookingAsync(plan, meal, request.Status, cancellationToken);
        meal.Status = request.Status;
        var stock = new PantrySnapshot(items, clock.Today(), (await settings.GetAdvancedAsync(cancellationToken)).ExpiringSoonDays);
        await ReconcileAsync(plan, stock, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse([]);
    }

    public async Task<DateTimeOffset?> NextScheduledStagingAsync(CancellationToken cancellationToken) =>
        PlanningEngine.NextScheduledStaging(await settings.GetPlannerAsync(cancellationToken), clock.GetLocalNow());

    private async Task<PlanResponse> StageAsync(
        DateOnly weekStart, bool replace, bool favorPantry, string trigger, PlanStyle style, IReadOnlyList<Guid> keep,
        CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var existing = await validation.WeekToStageAsync(weekStart, replace, cancellationToken);
        var stock = await SnapshotAsync(cancellationToken);
        var kept = existing?.Meals.Where(meal => keep.Contains(meal.Id) && !meal.IsLeftover).ToList() ?? [];

        var planner = await settings.GetPlannerAsync(cancellationToken);
        var advanced = await settings.GetAdvancedAsync(cancellationToken);
        var library = await recipes.ListActiveAsync(cancellationToken);
        var fans = (await favourites.ListForRecipesAsync(library.Select(recipe => recipe.Id).ToList(), cancellationToken))
            .GroupBy(favourite => favourite.RecipeId)
            .ToDictionary(group => group.Key, group => group.Count());
        var draft = planning.Plan(new PlanRequest(
            weekStart,
            library,
            await plans.RecipeIdsPlannedAsync(weekStart.AddDays(-advanced.AvoidRepeatDays), weekStart, cancellationToken),
            stock,
            clock.GetUtcNow(),
            // Staging the same week again should offer something different.
            Seed: existing is null ? null : Random.Shared.Next(),
            PantryBoost: favorPantry ? options.Weights.RegeneratePantryBoost : 1,
            Style: style,
            Kept: kept,
            Fans: fans,
            People: planner.People,
            MaxLeftoverDays: planner.MaxLeftoverDays,
            PerishableWithinDays: advanced.PerishableWithinDays,
            CookbookNames: (await cookbooks.ListAsync(cancellationToken)).ToDictionary(cookbook => cookbook.Id, cookbook => cookbook.Name)));

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
    /// <param name="maxLeftoverDays">How long leftovers keep. Read from the settings when not given.</param>
    private async Task ReconcileAsync(
        MealPlan plan, PantrySnapshot stock, CancellationToken cancellationToken, IReadOnlyList<GroceryStore>? stores = null, int? maxLeftoverDays = null)
    {
        stores ??= await settings.ListStoresAsync(cancellationToken);
        PlanningEngine.PlaceLeftovers(plan, maxLeftoverDays ?? (await settings.GetPlannerAsync(cancellationToken)).MaxLeftoverDays);
        var list = shopping.BuildList(plan, stock, stores);
        shopping.ApplyList(plan, list);
        await plans.SaveAsync(plan, cancellationToken);
        await messages.PublishAsync(new PlanChanged(plan.Id));
    }

    /// <summary>
    /// Announces that a meal was cooked, or that "cooked" was taken back; the food history follows. Called before its
    /// status changes. A meal marked cooked after its day counts as cooked on that day. The event remembers who had
    /// the recipe as a favourite then, so changing favourites later doesn't change the history.
    /// </summary>
    private async Task AnnounceCookingAsync(MealPlan plan, PlannedMeal meal, MealStatus status, CancellationToken cancellationToken)
    {
        if (status == MealStatus.Cooked)
        {
            var today = clock.Today();
            var late = meal.Date is { } day && day < today;
            var favouredBy = meal.RecipeId is Guid recipeId
                ? (await favourites.ListForRecipesAsync([recipeId], cancellationToken)).Select(favourite => favourite.UserName).ToList()
                : [];
            await messages.PublishAsync(new MealCooked(
                plan.Id, meal.Id, meal.RecipeId, meal.Slot, meal.Title, meal.IsLeftover,
                late ? meal.Date!.Value : today, late ? null : clock.GetUtcNow(), favouredBy));
        }
        else if (meal.Status == MealStatus.Cooked)
        {
            await messages.PublishAsync(new MealUncooked(plan.Id, meal.Id));
        }
    }

    /// <summary>
    /// Reconciles every plan whose week hasn't ended with these stores, and <paramref name="changed"/> too when it is
    /// given, whatever its week.
    /// </summary>
    private async Task ReconcileOpenPlansAsync(
        IReadOnlyList<GroceryStore> stores, MealPlan? changed, CancellationToken cancellationToken)
    {
        var stock = await SnapshotAsync(cancellationToken);
        var maxLeftoverDays = (await settings.GetPlannerAsync(cancellationToken)).MaxLeftoverDays;
        foreach (var plan in await plans.ListOpenAsync(clock.Today(), cancellationToken))
        {
            if (plan.Id != changed?.Id)
            {
                await ReconcileAsync(plan, stock, cancellationToken, stores, maxLeftoverDays);
            }
        }

        if (changed is not null)
        {
            await ReconcileAsync(changed, stock, cancellationToken, stores, maxLeftoverDays);
        }
    }

    private async Task<PantrySnapshot> SnapshotAsync(CancellationToken cancellationToken) =>
        new(await pantry.ListAsync(cancellationToken), clock.Today(), (await settings.GetAdvancedAsync(cancellationToken)).ExpiringSoonDays);
}
