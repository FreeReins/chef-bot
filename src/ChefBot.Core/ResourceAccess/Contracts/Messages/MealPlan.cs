using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A week of meals, snacks and sweets, with the shopping list that goes with it.</summary>
internal sealed class MealPlan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The Sunday the week starts on. There is one plan per week.</summary>
    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; } = PlanStatus.Staged;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>The plan style it was made with, as it was named then.</summary>
    public string StyleName { get; set; } = string.Empty;

    /// <summary>True when weekday lunches are planned from the leftovers of recent dinners. Copied from the style when the plan was made.</summary>
    public bool LeftoverLunches { get; set; } = true;

    /// <summary>
    /// How many people each breakfast, lunch and dinner is for. Taken from the planning settings when the plan is made,
    /// and again while it is staged if they change.
    /// </summary>
    public int People { get; set; } = PlannerSettings.DefaultPeople;

    public List<PlannedMeal> Meals { get; set; } = [];

    public List<ShoppingItem> ShoppingItems { get; set; } = [];
}
