namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// A way of planning a week: which days dinner is cooked, how many breakfasts, snacks, sweets and drinks to plan, and
/// rules about the recipes chosen. Households keep several and pick one each time they plan a week.
/// </summary>
/// <param name="Id">The style's id. Null for a starter style that hasn't been added, or a new one being saved.</param>
/// <param name="Name">What the style is called. Unique within the household.</param>
/// <param name="Description">What the style is for, in a sentence.</param>
/// <param name="IsDefault">True for the style used when none is chosen, including by the weekly schedule.</param>
/// <param name="CookingDays">The days dinner is cooked.</param>
/// <param name="LeftoverLunches">True to plan weekday lunches from the leftovers of recent dinners.</param>
/// <param name="Breakfasts">How many different breakfasts to spread across the weekdays.</param>
/// <param name="Snacks">How many snacks to plan, made once on any day.</param>
/// <param name="Sweets">How many sweets to plan, made once on any day.</param>
/// <param name="Drinks">How many drinks to plan, made once on any day.</param>
/// <param name="Rules">Rules about the recipes chosen, applied in order.</param>
public sealed record PlanStyleDetails(
    Guid? Id,
    string Name,
    string? Description,
    bool IsDefault,
    IReadOnlyList<DayOfWeek>? CookingDays,
    bool LeftoverLunches,
    int Breakfasts,
    int Snacks,
    int Sweets,
    int Drinks,
    IReadOnlyList<PlanRuleDetails>? Rules);
