using System.Globalization;
using System.Text;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// Works out what to tell the household and when: the "plan ready" message, the daily reminders, and which old
/// backups to delete. It is given the plan, pantry and settings and returns decisions, so every rule can be tested
/// without a clock or a network. Reads and saves nothing.
/// </summary>
internal sealed class NotificationEngine
{
    /// <summary>"A new plan is ready", sent whenever a week is staged.</summary>
    /// <param name="plan">The staged plan.</param>
    /// <param name="trigger">What staged it: "schedule", "manual" or "regenerate".</param>
    public Notification PlanReady(MealPlan plan, string trigger)
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

        return new Notification(NotificationTypes.PlanReady, PlanTitle(plan), DescribePlan(plan), new Dictionary<string, object?>
        {
            ["planId"] = plan.Id,
            ["weekStart"] = plan.WeekStart,
            ["status"] = plan.Status.ToString(),
            ["trigger"] = trigger,
            ["dinners"] = dinners,
            ["snacks"] = Titles(plan, MealSlot.Snack),
            ["sweets"] = Titles(plan, MealSlot.Sweet),
            ["drinks"] = Titles(plan, MealSlot.Drink),
            ["style"] = plan.StyleName,
            ["toBuy"] = plan.ShoppingItems.Count(item => !item.IsPurchased),
        });
    }

    /// <summary>The reminders for one day, in the order they should arrive.</summary>
    /// <param name="plan">The plan covering today or the next one coming, if any.</param>
    /// <param name="pantry">Everything in the pantry.</param>
    /// <param name="today">The day the reminders are for.</param>
    /// <param name="expiringSoonDays">Food expiring within this many days is worth a reminder.</param>
    public IReadOnlyList<Notification> Daily(MealPlan? plan, IReadOnlyList<PantryItem> pantry, DateOnly today, int expiringSoonDays)
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

    /// <summary>The sample sent from the Settings tab.</summary>
    public Notification Test() => new(NotificationTypes.Test, "ChefBot test", "Notifications from ChefBot are working.",
        new Dictionary<string, object?> { ["sentBy"] = "Settings" });

    /// <summary>Whether the household asked for this kind of notification.</summary>
    public bool IsWanted(NotificationSettings settings, string type) => type switch
    {
        NotificationTypes.PlanReady => settings.PlanReady,
        NotificationTypes.SlowCooker => settings.SlowCooker,
        NotificationTypes.MealsToday => settings.MealsToday,
        NotificationTypes.ExpiringFood => settings.ExpiringFood,
        NotificationTypes.Shopping => settings.Shopping,
        _ => true,
    };

    /// <summary>The backups that fall outside the retention rules. The newest backup is always kept.</summary>
    public IReadOnlyList<StoredBackup> ExpiredBackups(IEnumerable<StoredBackup> files, BackupSettings settings, DateTimeOffset now)
    {
        var newestFirst = files.OrderByDescending(file => file.CreatedAt).ToList();
        return newestFirst
            .Where((file, index) => index > 0 && (
                (settings.KeepCount > 0 && index >= settings.KeepCount) ||
                (settings.KeepDays > 0 && file.CreatedAt < now.AddDays(-settings.KeepDays))))
            .ToList();
    }

    /// <summary>The file name for a backup made at <paramref name="localNow"/>.</summary>
    public string BackupName(DateTimeOffset localNow) =>
        $"chefbot-backup-{localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.json.gz";

    private static string PlanTitle(MealPlan plan) =>
        $"Meals for the week of {plan.WeekStart.ToString("MMM d", CultureInfo.InvariantCulture)}";

    /// <summary>The plan as a short plain-text message.</summary>
    private static string DescribePlan(MealPlan plan)
    {
        var text = new StringBuilder();
        foreach (var dinner in plan.Meals.Where(meal => meal.Slot == MealSlot.Dinner).OrderBy(meal => meal.Date))
        {
            var day = dinner.Date?.DayOfWeek.ToString()[..3] ?? "Any";
            var note = dinner.Recipe?.IsSlowCooker == true ? " (slow cooker)" : string.Empty;
            text.AppendLine($"{day}: {dinner.Title}{note}");
        }

        AppendList(text, "Breakfast", plan, MealSlot.Breakfast);
        AppendList(text, "Snacks", plan, MealSlot.Snack);
        AppendList(text, "Sweets", plan, MealSlot.Sweet);
        AppendList(text, "Drinks", plan, MealSlot.Drink);

        var toBuy = plan.ShoppingItems.Count(item => !item.IsPurchased);
        text.Append($"To buy: {toBuy} items. Review, swap and confirm the plan in ChefBot.");
        return text.ToString();
    }

    private static void AppendList(StringBuilder text, string label, MealPlan plan, MealSlot slot)
    {
        var titles = plan.Meals.Where(meal => meal.Slot == slot).Select(meal => meal.Title).Distinct().ToList();
        if (titles.Count > 0)
        {
            text.AppendLine($"{label}: {string.Join(", ", titles)}");
        }
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
                ["ingredients"] = meals.SelectMany(meal => meal.Ingredients).Select(ingredient => ingredient.DisplayName).Distinct().ToList(),
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
