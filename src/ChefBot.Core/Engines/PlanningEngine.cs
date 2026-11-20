using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>Everything the planner needs to propose a week.</summary>
/// <param name="WeekStart">The Sunday the week starts on.</param>
/// <param name="Recipes">The recipes that may be planned.</param>
/// <param name="RecentRecipeIds">Recipes planned recently, avoided where possible.</param>
/// <param name="Pantry">What is on hand, so expiring food gets used.</param>
/// <param name="CreatedAt">The timestamp to record on the plan.</param>
/// <param name="Seed">Fixes the random choices. Null gives the same plan for the same week every time.</param>
/// <param name="PantryBoost">Above 1 to lean harder on food already in the pantry, as when rebuilding after shopping.</param>
internal sealed record PlanRequest(
    DateOnly WeekStart,
    IReadOnlyList<Recipe> Recipes,
    IReadOnlySet<Guid> RecentRecipeIds,
    PantrySnapshot Pantry,
    DateTimeOffset CreatedAt,
    int? Seed = null,
    double PantryBoost = 1);

/// <summary>A proposed week, plus anything the household should know about how it was put together.</summary>
internal sealed record PlanDraft(MealPlan Plan, IReadOnlyList<string> Warnings);

/// <summary>
/// Plans weeks: proposes dinners, leftover lunches, optional breakfasts, snacks and sweets; keeps leftover lunches
/// following the dinners; changes single meals; and decides when the weekly schedule is due. Reads and saves nothing.
/// </summary>
internal sealed class PlanningEngine(IngredientCatalog catalog, PlannerOptions options, WeeklyScheduleOptions schedule)
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

    /// <summary>
    /// Rebuilds the weekday leftover lunches from the dinners as they stand, then sizes each
    /// dinner to cover the lunches that depend on it. Lunches that were set by hand, cooked
    /// or skipped are left alone. Changes <paramref name="plan"/> in place.
    /// </summary>
    public void PlaceLeftovers(MealPlan plan)
    {
        plan.Meals.RemoveAll(meal => meal is { Slot: MealSlot.Lunch, IsLeftover: true, Status: MealStatus.Planned });

        var dinners = plan.Meals
            .Where(meal => meal is { Slot: MealSlot.Dinner, Date: not null } && meal.Status != MealStatus.Skipped)
            .OrderByDescending(meal => meal.Date)
            .ToList();

        foreach (var day in WeekLayout.Weekdays)
        {
            var date = WeekLayout.DateFor(plan.WeekStart, day);
            if (plan.Meals.Any(meal => meal.Slot == MealSlot.Lunch && meal.Date == date))
            {
                continue;
            }

            var earliest = date.AddDays(-options.MaxLeftoverDays);
            var source = dinners.FirstOrDefault(dinner => dinner.Date < date && dinner.Date >= earliest);
            if (source is not null)
            {
                plan.Meals.Add(LeftoverOf(source, date));
            }
        }

        foreach (var dinner in plan.Meals.Where(meal => meal is { Slot: MealSlot.Dinner, Status: MealStatus.Planned }))
        {
            var lunches = plan.Meals.Count(meal => meal.LeftoverOfMealId == dinner.Id);
            dinner.Portions = options.DinnerPortions + options.LunchPortions * lunches;
        }
    }

    /// <summary>Makes a meal a fresh cooking of <paramref name="recipe"/>.</summary>
    public void UseRecipe(PlannedMeal meal, Recipe recipe)
    {
        meal.Recipe = recipe;
        meal.RecipeId = recipe.Id;
        meal.Title = recipe.Name;
        meal.CustomIngredients.Clear();
        ResetAsFreshMeal(meal);
    }

    /// <summary>Makes a meal a free-form one, such as a grain, protein and vegetable bowl.</summary>
    public void UseCustom(PlannedMeal meal, string title, IEnumerable<PlannedMealIngredient> ingredients)
    {
        meal.Recipe = null;
        meal.RecipeId = null;
        meal.Title = title;
        meal.CustomIngredients.Clear();
        meal.CustomIngredients.AddRange(ingredients);
        ResetAsFreshMeal(meal);
    }

    /// <summary>
    /// The week the weekly schedule should stage now, or null when it isn't due. The schedule is due from its day
    /// and time until the week it plans for starts, once per week: <paramref name="lastStagedWeek"/> records that.
    /// </summary>
    public DateOnly? ScheduledWeekDue(DateTimeOffset localNow, DateOnly? lastStagedWeek)
    {
        if (!schedule.Enabled)
        {
            return null;
        }

        var lastFiring = schedule.NextOccurrence(localNow).AddDays(-7);
        var week = WeekLayout.UpcomingWeekStart(DateOnly.FromDateTime(lastFiring.DateTime));
        var today = DateOnly.FromDateTime(localNow.DateTime);

        // A firing that was missed (say the app was off) still counts until its week starts, but never later.
        return week == WeekLayout.UpcomingWeekStart(today) && week != lastStagedWeek ? week : null;
    }

    /// <summary>When the schedule next fires, or null when it is switched off.</summary>
    public DateTimeOffset? NextScheduledStaging(DateTimeOffset localNow) =>
        schedule.Enabled ? schedule.NextOccurrence(localNow) : null;

    private void ResetAsFreshMeal(PlannedMeal meal)
    {
        meal.Portions = options.PortionsFor(meal.Slot);
        meal.LeftoverOfMealId = null;
        meal.Status = MealStatus.Planned;
    }

    private static PlannedMeal ForRecipe(Recipe recipe, MealSlot slot, DateOnly? date, int portions) => new()
    {
        Slot = slot,
        Date = date,
        Recipe = recipe,
        RecipeId = recipe.Id,
        Title = recipe.Name,
        Portions = portions,
    };

    private static PlannedMeal LeftoverOf(PlannedMeal dinner, DateOnly date) => new()
    {
        Slot = MealSlot.Lunch,
        Date = date,
        RecipeId = dinner.RecipeId,
        Recipe = dinner.Recipe,
        Title = $"Leftovers: {dinner.Title}",
        LeftoverOfMealId = dinner.Id,
    };

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
            run.Plan.Meals.Add(ForRecipe(picks[day], MealSlot.Dinner, date, options.DinnerPortions));
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
            var meal = ForRecipe(recipe, MealSlot.Breakfast, date, options.BreakfastPortions);
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
            run.Plan.Meals.Add(ForRecipe(recipe, slot, date: null, options.PortionsFor(slot)));
        }
    }

    private void AddLeftoverLunches(Run run)
    {
        PlaceLeftovers(run.Plan);

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

/// <summary>
/// Picks one recipe per slot so the whole set scores as high as possible.
/// It starts from several random weeks and improves each one swap at a time,
/// which is fast enough for libraries of thousands of recipes.
/// </summary>
internal static class WeekSelector
{
    /// <param name="slots">What needs filling, for example the dinner days.</param>
    /// <param name="pools">The recipes allowed in each slot.</param>
    /// <param name="alongside">Recipes already chosen, which are scored with the picks and never picked again.</param>
    /// <param name="score">Scores a complete set of recipes.</param>
    /// <param name="random">Source of the random starting weeks.</param>
    /// <param name="restarts">How many starting weeks to try.</param>
    /// <returns>A recipe for every slot, or null when the pools can't fill them with different recipes.</returns>
    public static Dictionary<TSlot, Recipe>? Choose<TSlot>(
        IReadOnlyList<TSlot> slots,
        IReadOnlyDictionary<TSlot, IReadOnlyList<Recipe>> pools,
        IReadOnlyCollection<Recipe> alongside,
        Func<IEnumerable<Recipe>, double> score,
        Random random,
        int restarts = 12)
        where TSlot : notnull
    {
        var fixedMeals = alongside.ToList();
        Dictionary<TSlot, Recipe>? best = null;
        var bestScore = double.NegativeInfinity;

        for (var attempt = 0; attempt < restarts; attempt++)
        {
            var candidate = RandomStart(slots, pools, fixedMeals, random);
            if (candidate is null)
            {
                continue;
            }

            Improve(slots, pools, fixedMeals, score, candidate);
            var total = score(candidate.Values.Concat(fixedMeals));
            if (total > bestScore)
            {
                best = candidate;
                bestScore = total;
            }
        }

        return best;
    }

    private static Dictionary<TSlot, Recipe>? RandomStart<TSlot>(
        IReadOnlyList<TSlot> slots,
        IReadOnlyDictionary<TSlot, IReadOnlyList<Recipe>> pools,
        List<Recipe> fixedMeals,
        Random random)
        where TSlot : notnull
    {
        var taken = fixedMeals.Select(recipe => recipe.Id).ToHashSet();
        var start = new Dictionary<TSlot, Recipe>();

        // Fill the most restricted slots first so a small pool isn't used up elsewhere.
        foreach (var slot in slots.OrderBy(slot => pools[slot].Count))
        {
            var available = pools[slot].Where(recipe => !taken.Contains(recipe.Id)).ToList();
            if (available.Count == 0)
            {
                return null;
            }

            var pick = available[random.Next(available.Count)];
            start[slot] = pick;
            taken.Add(pick.Id);
        }

        return start;
    }

    private static void Improve<TSlot>(
        IReadOnlyList<TSlot> slots,
        IReadOnlyDictionary<TSlot, IReadOnlyList<Recipe>> pools,
        List<Recipe> fixedMeals,
        Func<IEnumerable<Recipe>, double> score,
        Dictionary<TSlot, Recipe> current)
        where TSlot : notnull
    {
        var comparer = EqualityComparer<TSlot>.Default;
        var improved = true;
        while (improved)
        {
            improved = false;
            foreach (var slot in slots)
            {
                var others = current
                    .Where(pair => !comparer.Equals(pair.Key, slot))
                    .Select(pair => pair.Value)
                    .Concat(fixedMeals)
                    .ToList();
                var taken = others.Select(recipe => recipe.Id).ToHashSet();
                var bestHere = score(others.Append(current[slot]));

                foreach (var option in pools[slot])
                {
                    if (taken.Contains(option.Id) || option.Id == current[slot].Id)
                    {
                        continue;
                    }

                    var trial = score(others.Append(option));
                    if (trial > bestHere + 1e-9)
                    {
                        current[slot] = option;
                        bestHere = trial;
                        improved = true;
                    }
                }
            }
        }
    }
}
