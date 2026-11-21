using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Utilities;

/// <summary>
/// Portions, freshness and how choices are scored. Bound from the "Planner" settings section. What a week holds (its
/// dinner days, breakfasts, snacks and rules) comes from the household's plan styles instead.
/// </summary>
internal sealed class PlannerOptions
{
    /// <summary>People eating dinner.</summary>
    public int DinnerPortions { get; set; } = 2;

    /// <summary>People eating leftovers for a weekday lunch.</summary>
    public int LunchPortions { get; set; } = 2;

    public int BreakfastPortions { get; set; } = 2;

    /// <summary>Used when an imported recipe doesn't say how many it serves.</summary>
    public decimal DefaultServings { get; set; } = 4;

    /// <summary>A lunch only uses a dinner cooked within this many days.</summary>
    public int MaxLeftoverDays { get; set; } = 3;

    /// <summary>Avoid repeating a recipe planned within this many days.</summary>
    public int AvoidRepeatDays { get; set; } = 28;

    /// <summary>Pantry items expiring within this many days are pushed into the plan first.</summary>
    public int ExpiringSoonDays { get; set; } = 3;

    /// <summary>Ingredients that keep no longer than this are worth sharing between meals.</summary>
    public int PerishableWithinDays { get; set; } = 21;

    public ScoreWeights Weights { get; set; } = new();

    public int PortionsFor(MealSlot slot) => slot switch
    {
        MealSlot.Breakfast => BreakfastPortions,
        MealSlot.Lunch => LunchPortions,
        MealSlot.Dinner => DinnerPortions,
        _ => 0, // Snacks, sweets and drinks are made once, as the recipe is written.
    };

    /// <summary>Fails fast on settings the planner can't work with.</summary>
    public void Validate()
    {
        if (DinnerPortions < 1 || LunchPortions < 0 || BreakfastPortions < 1 || DefaultServings <= 0)
        {
            throw new RuleViolationException("Planner portions and DefaultServings must be positive.");
        }

        if (new[] { MaxLeftoverDays, AvoidRepeatDays, ExpiringSoonDays }.Any(value => value < 0))
        {
            throw new RuleViolationException("Planner day ranges can't be negative.");
        }
    }
}
