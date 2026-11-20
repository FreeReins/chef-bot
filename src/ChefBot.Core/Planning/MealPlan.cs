using System.Text.Json.Serialization;
using ChefBot.Core.Common;
using ChefBot.Core.Shopping;

namespace ChefBot.Core.Planning;

[JsonConverter(typeof(JsonStringEnumConverter<PlanStatus>))]
public enum PlanStatus
{
    /// <summary>A proposal to review, shuffle and adjust. Nothing has been bought against it.</summary>
    Staged,

    /// <summary>The plan the household is shopping and cooking from.</summary>
    Confirmed,
}

/// <summary>A week of meals, snacks and sweets, with the shopping list that goes with it.</summary>
public sealed class MealPlan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The Sunday the week starts on. There is one plan per week.</summary>
    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; } = PlanStatus.Staged;

    public DateTimeOffset CreatedAt { get; set; }

    public List<PlannedMeal> Meals { get; set; } = [];

    public List<ShoppingItem> ShoppingItems { get; set; } = [];

    public DateOnly WeekEnd => WeekStart.AddDays(6);

    public PlannedMeal GetMeal(Guid mealId) =>
        Meals.FirstOrDefault(meal => meal.Id == mealId)
        ?? throw new NotFoundException($"This plan has no meal with id {mealId}.");

    public ShoppingItem GetShoppingItem(Guid itemId) =>
        ShoppingItems.FirstOrDefault(item => item.Id == itemId)
        ?? throw new NotFoundException($"This plan has no shopping item with id {itemId}.");

    public void Confirm() => Status = PlanStatus.Confirmed;

    /// <summary>
    /// Rebuilds the weekday leftover lunches from the dinners as they stand, then sizes each
    /// dinner to cover the lunches that depend on it. Lunches that were set by hand, cooked
    /// or skipped are left alone.
    /// </summary>
    public void RefreshLeftoverLunches(PlannerOptions options)
    {
        Meals.RemoveAll(meal => meal is { Slot: MealSlot.Lunch, IsLeftover: true, Status: MealStatus.Planned });

        var dinners = Meals
            .Where(meal => meal is { Slot: MealSlot.Dinner, Date: not null } && meal.Status != MealStatus.Skipped)
            .OrderByDescending(meal => meal.Date)
            .ToList();

        foreach (var day in WeekLayout.Weekdays)
        {
            var date = WeekLayout.DateFor(WeekStart, day);
            if (Meals.Any(meal => meal.Slot == MealSlot.Lunch && meal.Date == date))
            {
                continue;
            }

            var earliest = date.AddDays(-options.MaxLeftoverDays);
            var source = dinners.FirstOrDefault(dinner => dinner.Date < date && dinner.Date >= earliest);
            if (source is not null)
            {
                Meals.Add(PlannedMeal.LeftoverOf(source, date));
            }
        }

        foreach (var dinner in Meals.Where(meal => meal is { Slot: MealSlot.Dinner, Status: MealStatus.Planned }))
        {
            var lunches = Meals.Count(meal => meal.LeftoverOfMealId == dinner.Id);
            dinner.Portions = options.DinnerPortions + options.LunchPortions * lunches;
        }
    }

    /// <summary>Replaces everything still to buy. Purchased items are history and stay.</summary>
    public void ReplaceOpenShoppingItems(IEnumerable<ShoppingItem> items)
    {
        ShoppingItems.RemoveAll(item => !item.IsPurchased);
        ShoppingItems.AddRange(items);
    }
}
