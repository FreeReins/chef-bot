using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>Checks on plans and their meals: staging a week, replacing and moving meals, shopping for a plan.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>Checks a week can be staged, and returns the plan it would replace, if any.</summary>
    public async Task<MealPlan?> WeekToStageAsync(DateOnly weekStart, bool replace, CancellationToken cancellationToken)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Sunday)
        {
            throw new RuleViolationException("A plan's week must start on a Sunday.");
        }

        var existing = await plans.FindByWeekAsync(weekStart, cancellationToken);
        if (existing is { Status: PlanStatus.Confirmed } && !replace)
        {
            throw new ConflictException($"The week of {weekStart:yyyy-MM-dd} is already confirmed. Pass replace to start it over.");
        }

        return existing;
    }

    public static void CanBePlanned(Recipe recipe)
    {
        if (recipe.IsArchived)
        {
            throw new RuleViolationException($"{recipe.Name} has been archived and can't be planned.");
        }
    }

    public static void ReplaceRequest(ReplaceMealRequest request)
    {
        if (request is not ({ RecipeId: not null, Custom: null } or { RecipeId: null, Custom: not null }))
        {
            throw new RuleViolationException("Send either recipeId or custom, but not both.");
        }
    }

    public static void CustomMeal(string? title, IReadOnlyCollection<PlannedMealIngredient> ingredients)
    {
        if (string.IsNullOrWhiteSpace(title) || ingredients.Count == 0)
        {
            throw new RuleViolationException("A custom meal needs a title and at least one ingredient.");
        }
    }

    /// <summary>Purchases are recorded against the working plan only.</summary>
    public static void CanShopFor(MealPlan plan)
    {
        if (plan.Status != PlanStatus.Confirmed)
        {
            throw new ConflictException("Confirm the staged plan before recording purchases against it.");
        }
    }

    public static void NotYetPurchased(ShoppingItem item)
    {
        if (item.IsPurchased)
        {
            throw new ConflictException($"{item.DisplayName} is already marked as purchased.");
        }
    }

    /// <summary>
    /// Checks a meal can move to <paramref name="date"/>: it is a planned dinner, breakfast or lunch of its own (leftovers
    /// follow their dinner), the day is in the plan's week, and a meal it would swap with isn't cooked or skipped yet.
    /// </summary>
    public static void CanMove(MealPlan plan, PlannedMeal meal, DateOnly date)
    {
        if (meal.Date is null)
        {
            throw new RuleViolationException($"{meal.Title} can be made on any day, so it has no day to move.");
        }

        if (meal.IsLeftover)
        {
            throw new RuleViolationException("Leftover lunches follow their dinner. Move the dinner instead.");
        }

        if (meal.Status != MealStatus.Planned)
        {
            throw new ConflictException($"{meal.Title} is already marked as {meal.Status.ToString().ToLowerInvariant()}. Undo that first to move it.");
        }

        if (date < plan.WeekStart || date > WeekLayout.WeekEnd(plan.WeekStart))
        {
            throw new RuleViolationException("Meals can only move to another day of the same week.");
        }

        var other = plan.Meals.FirstOrDefault(other => other.Id != meal.Id && other.Slot == meal.Slot && other.Date == date && !other.IsLeftover);
        if (other is not null && other.Status != MealStatus.Planned)
        {
            throw new ConflictException($"{other.Title} on that day is already marked as {other.Status.ToString().ToLowerInvariant()}, so the two can't swap.");
        }
    }
}
