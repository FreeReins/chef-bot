using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A way of planning a week: the dinner days, how many of each other kind of meal, and rules about the recipes.
/// Saved per household, and offered as starter styles from config/plan-styles.json.
/// </summary>
internal sealed class PlanStyle
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>True for the style used when none is chosen, including by the weekly schedule.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Where it comes in the household's list of styles.</summary>
    public int Position { get; set; }

    /// <summary>The days dinner is cooked.</summary>
    public List<DayOfWeek> CookingDays { get; set; } = [];

    /// <summary>True to plan weekday lunches from the leftovers of recent dinners.</summary>
    public bool LeftoverLunches { get; set; } = true;

    /// <summary>How many different breakfasts to spread across the weekdays.</summary>
    public int Breakfasts { get; set; }

    public int Snacks { get; set; }

    public int Sweets { get; set; }

    public int Drinks { get; set; }

    /// <summary>Rules about the recipes chosen, applied in order.</summary>
    public List<PlanRule> Rules { get; set; } = [];

    /// <summary>
    /// The planner's own defaults, used when the household has saved no styles and no starter styles are installed:
    /// four dinners spaced for leftover lunches, one of them slow cooker on Tuesday, two breakfasts, three snacks and a sweet.
    /// </summary>
    public static PlanStyle Classic() => new()
    {
        Name = "Classic week",
        Description = "Four dinners with leftovers for weekday lunches, one slow cooker meal, breakfasts, snacks and a sweet.",
        IsDefault = true,
        CookingDays = [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday],
        LeftoverLunches = true,
        Breakfasts = 2,
        Snacks = 3,
        Sweets = 1,
        Rules =
        [
            new PlanRule
            {
                Limit = RuleLimit.AtLeast,
                Count = 1,
                Slot = MealSlot.Dinner,
                Match = RecipeMatchKind.SlowCooker,
                Day = DayOfWeek.Tuesday,
            },
        ],
    };
}
