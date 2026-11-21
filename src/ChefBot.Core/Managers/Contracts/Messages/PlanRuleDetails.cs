namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// One rule of a plan style, read as a sentence: "<paramref name="Limit"/> <paramref name="Count"/>
/// <paramref name="Slot"/> meals that match <paramref name="Match"/>", for example "at least 3 dinners that are slow
/// cooker" or "no meals that contain beef". A rule that can't be met, say for want of matching recipes, is relaxed and
/// the plan says so in its notes.
/// </summary>
/// <param name="Limit">How the matching meals are counted.</param>
/// <param name="Count">How many meals the limit means. Ignored for <see cref="RuleLimit.None"/>.</param>
/// <param name="Slot">The kind of meal the rule counts. Null means every kind, and is only allowed with <see cref="RuleLimit.None"/>.</param>
/// <param name="Match">What the rule looks for in a recipe.</param>
/// <param name="Values">The tags or ingredient names for <see cref="RecipeMatchKind.Tag"/> and <see cref="RecipeMatchKind.Ingredient"/>; any one matches.</param>
/// <param name="Minutes">The time limit for <see cref="RecipeMatchKind.ReadyWithin"/>.</param>
/// <param name="Day">The day matching dinners go on first, such as Tuesday for the slow cooker. Dinners only.</param>
public sealed record PlanRuleDetails(
    RuleLimit Limit,
    int Count,
    MealSlot? Slot,
    RecipeMatchKind Match,
    IReadOnlyList<string>? Values = null,
    int? Minutes = null,
    DayOfWeek? Day = null);
