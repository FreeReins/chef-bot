using ChefBot.Core.Common;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Planning;

/// <summary>Everything the planner needs to propose a week.</summary>
/// <param name="WeekStart">The Sunday the week starts on.</param>
/// <param name="Recipes">The recipes that may be planned.</param>
/// <param name="RecentRecipeIds">Recipes planned recently, avoided where possible.</param>
/// <param name="Pantry">What is on hand, so expiring food gets used.</param>
/// <param name="CreatedAt">The timestamp to record on the plan.</param>
/// <param name="Seed">Fixes the random choices. Null gives the same plan for the same week every time.</param>
/// <param name="PantryBoost">Above 1 to lean harder on food already in the pantry, as when rebuilding after shopping.</param>
public sealed record PlanRequest(
    DateOnly WeekStart,
    IReadOnlyList<Recipe> Recipes,
    IReadOnlySet<Guid> RecentRecipeIds,
    PantrySnapshot Pantry,
    DateTimeOffset CreatedAt,
    int? Seed = null,
    double PantryBoost = 1);

/// <summary>A proposed week, plus anything the household should know about how it was put together.</summary>
public sealed record PlanDraft(MealPlan Plan, IReadOnlyList<string> Warnings);

/// <summary>
/// Proposes a week of dinners, leftover lunches, optional breakfasts, snacks and sweets.
/// This class is pure logic: it reads nothing and saves nothing.
/// </summary>
public sealed class WeekPlanner(IngredientCatalog catalog, PlannerOptions options)
{
    private const string RepeatsAllowed = "Few recipes haven't been made recently, so some repeats were allowed.";

    public PlanDraft Plan(PlanRequest request)
    {
        var run = new Run(
            request,
            new WeekScorer(catalog, options, request.Pantry, request.WeekStart.Month, request.PantryBoost),
            new Random(request.Seed ?? request.WeekStart.DayNumber),
            new MealPlan { WeekStart = request.WeekStart, CreatedAt = request.CreatedAt },
            Chosen: [],
            Warnings: []);

        AddDinners(run);
        AddBreakfasts(run);
        AddExtras(run, MealSlot.Snack, RecipeCourse.Snack, options.Snacks);
        AddExtras(run, MealSlot.Sweet, RecipeCourse.Sweet, options.Sweets);
        AddLeftoverLunches(run);

        return new PlanDraft(run.Plan, run.Warnings.Distinct().ToList());
    }

    private void AddDinners(Run run)
    {
        var mains = run.Pool(RecipeCourse.Main);
        if (mains.Count == 0)
        {
            throw new RuleViolationException("There are no main-course recipes to plan. Add some, or import them from Mealie.");
        }

        var days = WeekLayout.DinnerDays(options.Dinners);
        var anyMain = PreferFresh(run, mains, days.Count);
        var pools = days.ToDictionary(day => day, _ => anyMain);

        var slowCookers = mains.Where(recipe => recipe.IsSlowCooker).ToList();
        if (options.SlowCookerMeals > 0 && slowCookers.Count == 0)
        {
            run.Warnings.Add("No slow cooker recipes were found, so this week has none. Mark some recipes as slow cooker.");
        }
        else
        {
            var freshSlowCookers = PreferFresh(run, slowCookers, options.SlowCookerMeals);
            foreach (var day in SlowCookerDays(days))
            {
                pools[day] = freshSlowCookers;
            }
        }

        var picks = WeekSelector.Choose(days, pools, run.Chosen, run.Scorer.Score, run.Random)
            ?? throw new RuleViolationException($"There aren't enough recipes to plan {days.Count} different dinners.");

        foreach (var day in days)
        {
            var date = WeekLayout.DateFor(run.Request.WeekStart, day);
            run.Plan.Meals.Add(PlannedMeal.ForRecipe(picks[day], MealSlot.Dinner, date, options.DinnerPortions));
            run.Chosen.Add(picks[day]);
        }
    }

    /// <summary>The preferred slow cooker day first, then other cooking days if more than one is wanted.</summary>
    private IEnumerable<DayOfWeek> SlowCookerDays(IReadOnlyList<DayOfWeek> dinnerDays) =>
        dinnerDays.OrderBy(day => day == options.SlowCookerDay ? 0 : 1).Take(options.SlowCookerMeals);

    private void AddBreakfasts(Run run)
    {
        var weekdays = WeekLayout.Weekdays;
        var breakfasts = PickSet(run, RecipeCourse.Breakfast, Math.Min(options.BreakfastRecipes, weekdays.Count));
        if (breakfasts.Count == 0)
        {
            if (options.BreakfastRecipes > 0)
            {
                run.Warnings.Add("No breakfast recipes were found, so breakfasts are blank.");
            }

            return;
        }

        // Spread the chosen recipes over the week in runs: A A A B B rather than A B A B A.
        for (var index = 0; index < weekdays.Count; index++)
        {
            var recipe = breakfasts[index * breakfasts.Count / weekdays.Count];
            var date = WeekLayout.DateFor(run.Request.WeekStart, weekdays[index]);
            var meal = PlannedMeal.ForRecipe(recipe, MealSlot.Breakfast, date, options.BreakfastPortions);
            meal.IsOptional = true;
            run.Plan.Meals.Add(meal);
        }
    }

    /// <summary>Snacks and sweets: made once each, on no particular day.</summary>
    private void AddExtras(Run run, MealSlot slot, RecipeCourse course, int count)
    {
        var picks = PickSet(run, course, count);
        if (picks.Count < count)
        {
            run.Warnings.Add($"Only {picks.Count} of {count} {slot.ToString().ToLowerInvariant()} recipes could be planned.");
        }

        foreach (var recipe in picks)
        {
            run.Plan.Meals.Add(PlannedMeal.ForRecipe(recipe, slot, date: null, options.PortionsFor(slot)));
        }
    }

    private void AddLeftoverLunches(Run run)
    {
        run.Plan.RefreshLeftoverLunches(options);

        var open = WeekLayout.Weekdays
            .Where(day => !run.Plan.Meals.Any(meal => meal.Slot == MealSlot.Lunch && meal.Date == WeekLayout.DateFor(run.Request.WeekStart, day)))
            .ToList();

        if (open.Count > 0)
        {
            run.Warnings.Add($"No leftovers are available for lunch on {string.Join(", ", open)}.");
        }
    }

    /// <summary>Chooses up to <paramref name="count"/> different recipes of a course that go well with the week so far.</summary>
    private IReadOnlyList<Recipe> PickSet(Run run, RecipeCourse course, int count)
    {
        var pool = run.Pool(course);
        count = Math.Min(count, pool.Count);
        if (count <= 0)
        {
            return [];
        }

        var candidates = PreferFresh(run, pool, count);
        var slots = Enumerable.Range(0, count).ToList();
        var pools = slots.ToDictionary(slot => slot, _ => candidates);
        var picks = WeekSelector.Choose(slots, pools, run.Chosen, run.Scorer.Score, run.Random);
        if (picks is null)
        {
            return [];
        }

        var chosen = slots.Select(slot => picks[slot]).ToList();
        run.Chosen.AddRange(chosen);
        return chosen;
    }

    /// <summary>Leaves out recently planned recipes when enough others remain.</summary>
    private static IReadOnlyList<Recipe> PreferFresh(Run run, IReadOnlyList<Recipe> pool, int needed)
    {
        var fresh = pool.Where(recipe => !run.Request.RecentRecipeIds.Contains(recipe.Id)).ToList();
        if (fresh.Count >= needed)
        {
            return fresh;
        }

        if (pool.Count > 0)
        {
            run.Warnings.Add(RepeatsAllowed);
        }

        return pool;
    }

    /// <summary>The state of one planning pass.</summary>
    private sealed record Run(
        PlanRequest Request,
        WeekScorer Scorer,
        Random Random,
        MealPlan Plan,
        List<Recipe> Chosen,
        List<string> Warnings)
    {
        public IReadOnlyList<Recipe> Pool(RecipeCourse course) =>
            Request.Recipes.Where(recipe => recipe.Course == course).ToList();
    }
}
