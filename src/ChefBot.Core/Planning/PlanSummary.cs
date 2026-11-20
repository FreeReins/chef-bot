using System.Globalization;
using System.Text;

namespace ChefBot.Core.Planning;

/// <summary>Writes a plan as a short plain-text message, for notifications.</summary>
public static class PlanSummary
{
    public static string Title(MealPlan plan) =>
        $"Meals for the week of {plan.WeekStart.ToString("MMM d", CultureInfo.InvariantCulture)}";

    public static string Describe(MealPlan plan)
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
}
