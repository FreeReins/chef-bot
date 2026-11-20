using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.Utilities;

/// <summary>How the week is laid out and how choices are scored. Bound from the "Planner" settings section.</summary>
internal sealed class PlannerOptions
{
    /// <summary>Dinners cooked per week: 3, 4 or 5. The days are chosen so weekday lunches can be leftovers.</summary>
    public int Dinners { get; set; } = 4;

    public int SlowCookerMeals { get; set; } = 1;

    public DayOfWeek SlowCookerDay { get; set; } = DayOfWeek.Tuesday;

    /// <summary>How many different breakfasts to spread across the weekdays.</summary>
    public int BreakfastRecipes { get; set; } = 2;

    public int Snacks { get; set; } = 3;

    public int Sweets { get; set; } = 1;

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

    /// <summary>
    /// Import from Mealie before the weekly schedule stages a plan. Planning continues from saved recipes if Mealie
    /// is down. Staging a plan by hand never waits for an import.
    /// </summary>
    public bool ImportFromMealieBeforePlanning { get; set; }

    public ScoreWeights Weights { get; set; } = new();

    public int PortionsFor(MealSlot slot) => slot switch
    {
        MealSlot.Breakfast => BreakfastPortions,
        MealSlot.Lunch => LunchPortions,
        MealSlot.Dinner => DinnerPortions,
        _ => 0, // Snacks and sweets are made once, as the recipe is written.
    };

    /// <summary>Fails fast on settings the planner can't work with.</summary>
    public void Validate()
    {
        if (Dinners is < 3 or > 5)
        {
            throw new RuleViolationException("Planner:Dinners must be 3, 4 or 5.");
        }

        if (DinnerPortions < 1 || LunchPortions < 0 || BreakfastPortions < 1 || DefaultServings <= 0)
        {
            throw new RuleViolationException("Planner portions and DefaultServings must be positive.");
        }

        if (new[] { SlowCookerMeals, BreakfastRecipes, Snacks, Sweets, MaxLeftoverDays, AvoidRepeatDays, ExpiringSoonDays }.Any(value => value < 0))
        {
            throw new RuleViolationException("Planner counts and day ranges can't be negative.");
        }
    }
}
