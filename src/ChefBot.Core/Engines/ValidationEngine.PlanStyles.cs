using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <content>Checks on plan styles and their rules.</content>
internal sealed partial class ValidationEngine
{
    /// <summary>
    /// Checks a new list of plan styles: each has a name of its own, plans something, has counts the week can hold,
    /// and has rules that say what they look for. When no style is marked as the default, the first one becomes it.
    /// </summary>
    public static IReadOnlyList<PlanStyleDetails> PlanStyles(IReadOnlyList<PlanStyleDetails>? styles)
    {
        var list = styles ?? [];
        if (list.Count > MaxPlanStyles)
        {
            throw new RuleViolationException($"Keep to {MaxPlanStyles} plan styles or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in list)
        {
            var name = style.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every plan style needs a name.");
            }

            if (name.Length > MaxPlanStyleName)
            {
                throw new RuleViolationException($"Keep plan style names to {MaxPlanStyleName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two plan styles called {name}. Give each one its own name.");
            }

            PlanStyle(name, style);
        }

        if (list.Count(style => style.IsDefault) > 1)
        {
            throw new RuleViolationException("Only one plan style can be the default. Choose one and untick the others.");
        }

        return list.Count > 0 && !list.Any(style => style.IsDefault)
            ? [list[0] with { IsDefault = true }, .. list.Skip(1)]
            : list;
    }

    private static void PlanStyle(string name, PlanStyleDetails style)
    {
        var days = style.CookingDays ?? [];
        if (days.Any(day => !Enum.IsDefined(day)) || days.Distinct().Count() != days.Count)
        {
            throw new RuleViolationException($"{name} lists a cooking day twice. Choose each day once.");
        }

        if (style.Breakfasts is < 0 or > 5)
        {
            throw new RuleViolationException($"{name} can plan 0 to 5 different breakfasts, one per weekday at most.");
        }

        if (new[] { style.Snacks, style.Sweets, style.Drinks }.Any(count => count is < 0 or > MaxExtras))
        {
            throw new RuleViolationException($"{name} can plan 0 to {MaxExtras} each of snacks, sweets and drinks.");
        }

        if (days.Count + style.Breakfasts + style.Snacks + style.Sweets + style.Drinks == 0)
        {
            throw new RuleViolationException($"{name} doesn't plan anything. Choose some cooking days, or some breakfasts, snacks, sweets or drinks.");
        }

        var rules = style.Rules ?? [];
        if (rules.Count > MaxPlanRules)
        {
            throw new RuleViolationException($"{name} has more than {MaxPlanRules} rules. Remove some.");
        }

        foreach (var rule in rules)
        {
            PlanRule(name, rule);
        }
    }

    private static void PlanRule(string name, PlanRuleDetails rule)
    {
        if (!Enum.IsDefined(rule.Limit) || !Enum.IsDefined(rule.Match) || (rule.Slot is { } slot && !Enum.IsDefined(slot)))
        {
            throw new RuleViolationException($"{name} has a rule ChefBot doesn't understand. Remove it and add it again.");
        }

        if (rule.Slot is null && rule.Limit != RuleLimit.None)
        {
            throw new RuleViolationException($"{name} has a rule for every kind of meal that isn't \"no meals\". Choose a kind of meal, such as dinners, for counted rules.");
        }

        if (rule.Slot == MealSlot.Lunch)
        {
            throw new RuleViolationException($"{name} has a rule for lunches, but lunches are leftovers of dinners. Make it a rule for dinners instead.");
        }

        if (rule.Limit != RuleLimit.None && rule.Count is < 0 or > MaxExtras)
        {
            throw new RuleViolationException($"{name} has a rule counting {rule.Count} meals. Use a number from 0 to {MaxExtras}.");
        }

        if (rule.Match is RecipeMatchKind.Tag or RecipeMatchKind.Ingredient && !(rule.Values ?? []).Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            var what = rule.Match == RecipeMatchKind.Tag ? "tag" : "ingredient";
            throw new RuleViolationException($"{name} has a rule that needs at least one {what}. Type one, or remove the rule.");
        }

        if (rule.Match == RecipeMatchKind.Cookbook && ((rule.Values ?? []).Count == 0 || !(rule.Values ?? []).All(value => Guid.TryParse(value, out _))))
        {
            throw new RuleViolationException($"{name} has a cookbook rule without a cookbook. Choose one, or remove the rule.");
        }

        if (rule.Match == RecipeMatchKind.ReadyWithin && rule.Minutes is not (> 0 and <= MaxRuleMinutes))
        {
            throw new RuleViolationException($"{name} has a time rule without a sensible number of minutes. Use a number from 1 to {MaxRuleMinutes}.");
        }

        if (rule.Day is not null && (rule.Slot != MealSlot.Dinner || rule.Limit is RuleLimit.AtMost or RuleLimit.None || !Enum.IsDefined(rule.Day.Value)))
        {
            throw new RuleViolationException($"{name} has a rule with a day that isn't for dinners. Only \"at least\" and \"exactly\" dinner rules can choose a day.");
        }
    }

    /// <summary>The household's style with this id, or null to use the default.</summary>
    public static PlanStyle? StyleWithId(IReadOnlyList<PlanStyle> styles, IReadOnlyList<PlanStyle> starters, Guid? id) =>
        id is not Guid wanted
            ? null
            : styles.Concat(starters).FirstOrDefault(style => style.Id == wanted)
              ?? throw new NotFoundException("That plan style no longer exists. Reload the page and choose another.");
}
