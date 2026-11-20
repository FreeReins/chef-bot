using System.Globalization;
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

/// <summary>Points added to or taken from a candidate week. Positive is better.</summary>
internal sealed class ScoreWeights
{
    /// <summary>Per extra meal that uses the same perishable ingredient.</summary>
    public double SharedPerishable { get; set; } = 3;

    /// <summary>A perishable only one meal uses, and that isn't already in the pantry.</summary>
    public double StrandedPerishable { get; set; } = -1;

    /// <summary>A recipe whose seasonal produce is all in season scores this; all out of season scores the negative.</summary>
    public double Seasonal { get; set; } = 3;

    /// <summary>Per ingredient already in the pantry.</summary>
    public double OnHand { get; set; } = 1.5;

    /// <summary>Extra, per pantry ingredient that is about to expire.</summary>
    public double ExpiringSoon { get; set; } = 4;

    /// <summary>Per meal beyond two built on the same protein.</summary>
    public double SameProtein { get; set; } = -3;

    /// <summary>How much more pantry food counts when a plan is rebuilt around what was already bought.</summary>
    public double RegeneratePantryBoost { get; set; } = 4;

    /// <summary>Per ingredient a swap suggestion would add to the shopping list.</summary>
    public double MissingIngredient { get; set; } = -0.5;
}

/// <summary>
/// The words that sort imported recipes into courses. They are matched as whole
/// words against a recipe's tags, categories and tools. Values come from appsettings.json.
/// </summary>
internal sealed class ClassifierOptions
{
    public List<string> BreakfastWords { get; set; } = [];

    public List<string> SnackWords { get; set; } = [];

    public List<string> SweetWords { get; set; } = [];

    /// <summary>Also matched against the recipe name.</summary>
    public List<string> SlowCookerWords { get; set; } = [];

    /// <summary>Labels for things that are never a meal on their own, such as sides and drinks.</summary>
    public List<string> NotAMealWords { get; set; } = [];

    /// <summary>When set, only recipes carrying one of these labels count as mains.</summary>
    public List<string> MainWords { get; set; } = [];
}

/// <summary>Where the household shops. Bound from the "Stores" settings section.</summary>
internal sealed class StoreOptions
{
    /// <summary>The store for anything without a better match.</summary>
    public string Default { get; set; } = "Grocery store";

    /// <summary>Where in-season produce is sent during market months. Empty turns this off.</summary>
    public string FarmersMarket { get; set; } = string.Empty;

    /// <summary>Months (1-12) the farmers market is worth a trip.</summary>
    public List<int> FarmersMarketMonths { get; set; } = [];
}

/// <summary>When next week's plan is staged automatically. Bound from the "Schedule" settings section.</summary>
internal sealed class WeeklyScheduleOptions
{
    public bool Enabled { get; set; } = true;

    public DayOfWeek Day { get; set; } = DayOfWeek.Saturday;

    /// <summary>Local time of day, as HH:mm.</summary>
    public string Time { get; set; } = "09:00";

    /// <summary>The next moment the schedule fires, strictly after <paramref name="localNow"/>.</summary>
    public DateTimeOffset NextOccurrence(DateTimeOffset localNow)
    {
        var time = TimeOnly.TryParseExact(Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : new TimeOnly(9, 0);

        var daysAhead = ((int)Day - (int)localNow.DayOfWeek + 7) % 7;
        var candidate = new DateTimeOffset(localNow.Date.AddDays(daysAhead) + time.ToTimeSpan(), localNow.Offset);
        return candidate > localNow ? candidate : candidate.AddDays(7);
    }
}
