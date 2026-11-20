using System.Globalization;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Notifications;

/// <summary>
/// Works out which notifications apply. Pure logic: it is given the plan and the pantry
/// and returns messages, so every rule can be tested without a clock or a network.
/// </summary>
public static class ReminderBuilder
{
    /// <summary>"A new plan is ready", sent whenever a week is staged.</summary>
    /// <param name="plan">The staged plan.</param>
    /// <param name="trigger">What staged it: "schedule", "manual" or "regenerate".</param>
    public static Notification PlanReady(MealPlan plan, string trigger)
    {
        var dinners = plan.Meals
            .Where(meal => meal.Slot == MealSlot.Dinner)
            .OrderBy(meal => meal.Date)
            .Select(meal => new Dictionary<string, object?>
            {
                ["day"] = meal.Date?.DayOfWeek.ToString(),
                ["date"] = meal.Date,
                ["title"] = meal.Title,
                ["slowCooker"] = meal.Recipe?.IsSlowCooker == true,
            })
            .ToList();

        return new Notification(NotificationTypes.PlanReady, PlanSummary.Title(plan), PlanSummary.Describe(plan), new Dictionary<string, object?>
        {
            ["planId"] = plan.Id,
            ["weekStart"] = plan.WeekStart,
            ["status"] = plan.Status.ToString(),
            ["trigger"] = trigger,
            ["dinners"] = dinners,
            ["snacks"] = Titles(plan, MealSlot.Snack),
            ["sweets"] = Titles(plan, MealSlot.Sweet),
            ["toBuy"] = plan.ShoppingItems.Count(item => !item.IsPurchased),
        });
    }

    /// <summary>The reminders for one day, in the order they should arrive.</summary>
    /// <param name="plan">The plan covering today or the next one coming, if any.</param>
    /// <param name="pantry">Everything in the pantry.</param>
    /// <param name="today">The day the reminders are for.</param>
    /// <param name="expiringSoonDays">Food expiring within this many days is worth a reminder.</param>
    public static IReadOnlyList<Notification> Daily(MealPlan? plan, IReadOnlyList<PantryItem> pantry, DateOnly today, int expiringSoonDays)
    {
        var reminders = new List<Notification?>
        {
            SlowCooker(plan, today),
            MealsToday(plan, today),
            ExpiringFood(plan, pantry, today, expiringSoonDays),
            Shopping(plan, today),
        };

        return reminders.OfType<Notification>().ToList();
    }

    private static Notification? SlowCooker(MealPlan? plan, DateOnly today)
    {
        var meals = TodaysMeals(plan, today).Where(meal => meal.Recipe?.IsSlowCooker == true && !meal.IsLeftover).ToList();
        if (meals.Count == 0)
        {
            return null;
        }

        var names = string.Join(" and ", meals.Select(meal => meal.Title));
        return new Notification(NotificationTypes.SlowCooker, "Start the slow cooker", $"Tonight is {names}. Get the slow cooker going this morning.",
            new Dictionary<string, object?>
            {
                ["date"] = today,
                ["meals"] = meals.Select(meal => meal.Title).ToList(),
                ["ingredients"] = meals.SelectMany(meal => meal.GetIngredients()).Select(ingredient => ingredient.DisplayName).Distinct().ToList(),
            });
    }

    private static Notification? MealsToday(MealPlan? plan, DateOnly today)
    {
        var meals = TodaysMeals(plan, today).OrderBy(meal => meal.Slot).ToList();
        if (meals.Count == 0)
        {
            return null;
        }

        var lines = meals.Select(meal => $"{meal.Slot}: {meal.Title}").ToList();
        return new Notification(NotificationTypes.MealsToday, "Today's meals", string.Join(". ", lines) + ".", new Dictionary<string, object?>
        {
            ["date"] = today,
            ["meals"] = meals.Select(meal => new Dictionary<string, object?>
            {
                ["slot"] = meal.Slot.ToString(),
                ["title"] = meal.Title,
                ["optional"] = meal.IsOptional,
                ["leftover"] = meal.IsLeftover,
            }).ToList(),
        });
    }

    private static Notification? ExpiringFood(MealPlan? plan, IReadOnlyList<PantryItem> pantry, DateOnly today, int expiringSoonDays)
    {
        var expiring = pantry
            .Where(item => item.FreshnessOn(today, expiringSoonDays) == Freshness.ExpiringSoon)
            .OrderBy(item => item.ExpiresOn)
            .ToList();
        if (expiring.Count == 0)
        {
            return null;
        }

        var planned = (plan is null ? [] : MealRequirements.For(plan.Meals))
            .Select(need => need.Ingredient.Key)
            .ToHashSet(StringComparer.Ordinal);
        var unplanned = expiring.Where(item => !planned.Contains(item.Key)).Select(item => item.DisplayName).Distinct().ToList();
        var names = string.Join(", ", expiring.Select(item => $"{item.DisplayName} ({Short(item.ExpiresOn!.Value)})").Distinct());
        var advice = unplanned.Count > 0
            ? $" Nothing on the plan uses {string.Join(", ", unplanned)}: swap a meal to use it up."
            : string.Empty;

        return new Notification(NotificationTypes.ExpiringFood, "Food to use soon", $"Expiring soon: {names}.{advice}", new Dictionary<string, object?>
        {
            ["date"] = today,
            ["items"] = expiring.Select(item => new Dictionary<string, object?>
            {
                ["name"] = item.DisplayName,
                ["expiresOn"] = item.ExpiresOn,
                ["daysLeft"] = item.DaysLeft(today),
                ["usedByPlan"] = planned.Contains(item.Key),
            }).ToList(),
        });
    }

    /// <summary>The day before a week starts and on its first day: confirm the plan, or finish the shopping.</summary>
    private static Notification? Shopping(MealPlan? plan, DateOnly today)
    {
        if (plan is null || plan.WeekStart.DayNumber - today.DayNumber is not (0 or 1))
        {
            return null;
        }

        var toBuy = plan.ShoppingItems.Where(item => !item.IsPurchased).ToList();
        var data = new Dictionary<string, object?>
        {
            ["planId"] = plan.Id,
            ["weekStart"] = plan.WeekStart,
            ["status"] = plan.Status.ToString(),
            ["toBuy"] = toBuy.Select(item => item.DisplayName).ToList(),
        };

        if (plan.Status == PlanStatus.Staged)
        {
            return new Notification(NotificationTypes.Shopping, "The plan needs confirming",
                $"The plan for the week of {Short(plan.WeekStart)} hasn't been confirmed yet. Review it, then shop.", data);
        }

        return toBuy.Count == 0
            ? null
            : new Notification(NotificationTypes.Shopping, "Groceries still to buy",
                $"{toBuy.Count} items are still on the list for the week of {Short(plan.WeekStart)}.", data);
    }

    private static IEnumerable<PlannedMeal> TodaysMeals(MealPlan? plan, DateOnly today) =>
        plan?.Meals.Where(meal => meal.Date == today && meal.Status == MealStatus.Planned) ?? [];

    private static List<string> Titles(MealPlan plan, MealSlot slot) =>
        plan.Meals.Where(meal => meal.Slot == slot).Select(meal => meal.Title).Distinct().ToList();

    private static string Short(DateOnly date) => date.ToString("MMM d", CultureInfo.InvariantCulture);
}
