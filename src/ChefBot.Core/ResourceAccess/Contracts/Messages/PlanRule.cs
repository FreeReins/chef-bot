using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One rule of a plan style, such as "at least 3 dinners that are slow cooker". See <see cref="PlanRuleDetails"/>.</summary>
internal sealed class PlanRule
{
    public RuleLimit Limit { get; set; }

    public int Count { get; set; }

    /// <summary>The kind of meal counted. Null for every kind, with <see cref="RuleLimit.None"/> only.</summary>
    public MealSlot? Slot { get; set; }

    public RecipeMatchKind Match { get; set; }

    /// <summary>Tags or ingredient names; any one matches.</summary>
    public List<string> Values { get; set; } = [];

    public int? Minutes { get; set; }

    /// <summary>The day matching dinners go on first.</summary>
    public DayOfWeek? Day { get; set; }
}
