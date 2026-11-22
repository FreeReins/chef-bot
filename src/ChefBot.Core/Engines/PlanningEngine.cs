using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Plans weeks in a household's plan style: proposes dinners on its cooking days, leftover lunches, optional
/// breakfasts, snacks, sweets and drinks, following the style's rules about recipes; keeps leftover lunches following
/// the dinners; changes and moves single meals; and decides when the weekly schedule is due. Reads and saves nothing.
/// </summary>
internal sealed partial class PlanningEngine(IngredientCatalog catalog, PlannerOptions options)
{
    private const string RepeatsAllowed = "Few recipes haven't been made recently, so some repeats were allowed.";

    public PlanDraft Plan(PlanRequest request)
    {
        var style = request.Style ?? PlanStyle.Classic();
        var run = new Run(
            request,
            style,
            new WeekScorer(
                catalog, options, request.Pantry, request.WeekStart.Month, request.PantryBoost, request.Fans, FavouriteWeight(style.Favourites),
                request.PerishableWithinDays),
            new Random(request.Seed ?? request.WeekStart.DayNumber),
            new MealPlan
            {
                WeekStart = request.WeekStart,
                CreatedAt = request.CreatedAt,
                StyleName = style.Name,
                LeftoverLunches = style.LeftoverLunches,
                People = request.People,
            },
            Chosen: [],
            Warnings: []);

        KeepMeals(run);
        AddDinners(run);
        AddBreakfasts(run);
        AddExtras(run, MealSlot.Snack, RecipeCourse.Snack, style.Snacks);
        AddExtras(run, MealSlot.Sweet, RecipeCourse.Sweet, style.Sweets);
        AddExtras(run, MealSlot.Drink, RecipeCourse.Drink, style.Drinks);
        AddLeftoverLunches(run);

        return new PlanDraft(run.Plan, run.Warnings.Distinct().ToList());
    }

    /// <summary>
    /// Moves a meal to another day. A meal of the same kind already on that day takes the moved meal's old day, so
    /// the two swap. Returns the meal that was swapped, if any. Leftover lunches follow once the plan is reconciled.
    /// </summary>
    public static PlannedMeal? Move(MealPlan plan, PlannedMeal meal, DateOnly date)
    {
        var other = plan.Meals.FirstOrDefault(other =>
            other.Id != meal.Id && other.Slot == meal.Slot && other.Date == date && !other.IsLeftover);
        if (other is not null)
        {
            other.Date = meal.Date;
        }

        meal.Date = date;
        return other;
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

    /// <summary>Copies the meals kept from an earlier plan of the week into the new one, as fresh planned meals.</summary>
    private void KeepMeals(Run run)
    {
        foreach (var kept in run.Request.Kept ?? [])
        {
            if (kept.IsLeftover)
            {
                continue;
            }

            run.Plan.Meals.Add(new PlannedMeal
            {
                Slot = kept.Slot,
                Date = kept.Date,
                Recipe = kept.Recipe,
                RecipeId = kept.RecipeId,
                Title = kept.Title,
                Portions = kept.PortionsChosen ? kept.Portions : PortionsFor(kept.Slot, run.Request.People),
                PortionsChosen = kept.PortionsChosen,
                IsOptional = kept.IsOptional,
                CustomIngredients = kept.CustomIngredients
                    .Select(ingredient => new PlannedMealIngredient { Key = ingredient.Key, CatalogName = ingredient.CatalogName, DisplayName = ingredient.DisplayName })
                    .ToList(),
            });

            if (kept.Recipe is not null && run.Chosen.All(recipe => recipe.Id != kept.Recipe.Id))
            {
                run.Chosen.Add(kept.Recipe);
            }
        }
    }

    private void AddDinners(Run run)
    {
        var kept = run.Plan.Meals.Where(meal => meal.Slot == MealSlot.Dinner).ToList();
        var days = OpenCookingDays(run, kept);
        if (days.Count == 0)
        {
            return;
        }

        if (run.Pool(RecipeCourse.Main).Count == 0)
        {
            throw new RuleViolationException("There are no main-course recipes to plan. Add some on the Recipes tab, or import a recipe file there.");
        }

        var picks = Fill(run, MealSlot.Dinner, RecipeCourse.Main, days, KeptRecipes(kept), day => day)
            ?? throw new RuleViolationException($"There aren't enough recipes to plan {days.Count + kept.Count} different dinners.");

        foreach (var day in days)
        {
            var date = WeekLayout.DateFor(run.Request.WeekStart, day);
            run.Plan.Meals.Add(ForRecipe(picks[day], MealSlot.Dinner, date, run.Request.People));
            run.Chosen.Add(picks[day]);
        }
    }

    /// <summary>
    /// The style's cooking days that still need a dinner. A dinner kept on another day stands in for the nearest
    /// cooking day, so moving a dinner and shuffling doesn't add a dinner.
    /// </summary>
    private static List<DayOfWeek> OpenCookingDays(Run run, IReadOnlyList<PlannedMeal> kept)
    {
        var open = run.Style.CookingDays.Distinct().Order().ToList();
        foreach (var day in kept.Where(meal => meal.Date is not null).Select(meal => meal.Date!.Value.DayOfWeek))
        {
            if (!open.Remove(day) && open.Count > 0)
            {
                open.Remove(open.MinBy(cookingDay => Math.Abs(cookingDay - day)));
            }
        }

        return open;
    }

    private void AddBreakfasts(Run run)
    {
        var kept = run.Plan.Meals.Where(meal => meal.Slot == MealSlot.Breakfast).ToList();
        var keptRecipes = KeptRecipes(kept);
        var weekdays = WeekLayout.Weekdays
            .Where(day => kept.All(meal => meal.Date != WeekLayout.DateFor(run.Request.WeekStart, day)))
            .ToList();
        var wanted = Math.Min(run.Style.Breakfasts, WeekLayout.Weekdays.Count);
        if (weekdays.Count == 0 || wanted == 0)
        {
            return;
        }

        var breakfasts = keptRecipes.Take(wanted).Concat(PickSet(run, MealSlot.Breakfast, RecipeCourse.Breakfast, wanted - keptRecipes.Count, keptRecipes)).ToList();
        if (breakfasts.Count == 0)
        {
            run.Warnings.Add("No breakfast recipes were found, so breakfasts are blank.");
            return;
        }

        // Spread the chosen recipes over the week in runs: A A A B B rather than A B A B A.
        for (var index = 0; index < weekdays.Count; index++)
        {
            var recipe = breakfasts[index * breakfasts.Count / weekdays.Count];
            var date = WeekLayout.DateFor(run.Request.WeekStart, weekdays[index]);
            var meal = ForRecipe(recipe, MealSlot.Breakfast, date, run.Request.People);
            meal.IsOptional = true;
            run.Plan.Meals.Add(meal);
        }
    }

    /// <summary>Snacks, sweets and drinks: made once each, on no particular day.</summary>
    private void AddExtras(Run run, MealSlot slot, RecipeCourse course, int count)
    {
        var kept = run.Plan.Meals.Where(meal => meal.Slot == slot).ToList();
        var wanted = count - kept.Count;
        if (wanted <= 0)
        {
            return;
        }

        var picks = PickSet(run, slot, course, wanted, KeptRecipes(kept));
        if (picks.Count < wanted)
        {
            run.Warnings.Add($"Only {picks.Count + kept.Count} of {count} {RecipeMatcher.Plural(slot)} could be planned. Add more {slot.ToString().ToLowerInvariant()} recipes.");
        }

        foreach (var recipe in picks)
        {
            run.Plan.Meals.Add(ForRecipe(recipe, slot, date: null, PortionsFor(slot, run.Request.People)));
        }
    }

    private void AddLeftoverLunches(Run run)
    {
        PlaceLeftovers(run.Plan, run.Request.MaxLeftoverDays);
        if (!run.Style.LeftoverLunches || !run.Plan.Meals.Any(meal => meal.Slot == MealSlot.Dinner))
        {
            return;
        }

        var open = WeekLayout.Weekdays
            .Where(day => !run.Plan.Meals.Any(meal => meal.Slot == MealSlot.Lunch && meal.Date == WeekLayout.DateFor(run.Request.WeekStart, day)))
            .ToList();

        if (open.Count > 0)
        {
            run.Warnings.Add($"No leftovers are available for lunch on {string.Join(", ", open)}.");
        }
    }

    /// <summary>Chooses up to <paramref name="count"/> different recipes of a course that go well with the week so far.</summary>
    private List<Recipe> PickSet(Run run, MealSlot slot, RecipeCourse course, int count, IReadOnlyList<Recipe> kept)
    {
        count = Math.Min(count, run.Pool(course).Count(recipe => run.Chosen.All(chosen => chosen.Id != recipe.Id)));
        if (count <= 0)
        {
            return [];
        }

        var slots = Enumerable.Range(0, count).ToList();
        var picks = Fill(run, slot, course, slots, kept, _ => null);
        if (picks is null)
        {
            return [];
        }

        var chosen = slots.Select(index => picks[index]).ToList();
        run.Chosen.AddRange(chosen);
        return chosen;
    }

    /// <summary>
    /// Picks a recipe for every open slot of one kind of meal, following the style's rules for that kind. Each rule
    /// narrows some slots' choices: "at least 3 slow cooker dinners" lets 3 dinner days choose only slow cooker
    /// recipes, preferring the rule's day; "exactly" also keeps such recipes off the other days; "at most" and "no"
    /// keep them off enough days. Meals kept from an earlier plan count towards the rules. A rule that can't be met is
    /// relaxed, last rule first, and the plan's notes say so. Null when the slots can't be filled with different
    /// recipes even with every rule relaxed.
    /// </summary>
    private Dictionary<TSlot, Recipe>? Fill<TSlot>(
        Run run, MealSlot slot, RecipeCourse course, IReadOnlyList<TSlot> open, IReadOnlyList<Recipe> kept, Func<TSlot, DayOfWeek?> dayOf)
        where TSlot : notnull
    {
        var pool = run.Pool(course).Where(recipe => run.Chosen.All(chosen => chosen.Id != recipe.Id)).ToList();
        var rules = run.Style.Rules.Where(rule => rule.Slot == slot || rule.Slot is null).ToList();
        var filters = open.ToDictionary(key => key, _ => new List<Filter>());
        var total = open.Count + kept.Count;

        foreach (var rule in rules)
        {
            var keptMatching = kept.Count(recipe => RecipeMatcher.Matches(recipe, rule, run.Request.Fans));
            var count = Math.Clamp(rule.Count, 0, total);
            var (wantMatching, wantOther) = rule.Limit switch
            {
                RuleLimit.AtLeast => (count - keptMatching, 0),
                RuleLimit.Exactly => (count - keptMatching, total - count - (kept.Count - keptMatching)),
                RuleLimit.AtMost => (0, total - count - (kept.Count - keptMatching)),
                _ => (0, open.Count),
            };
            wantMatching = Math.Clamp(wantMatching, 0, open.Count);
            wantOther = Math.Clamp(wantOther, 0, open.Count - wantMatching);

            var matching = Reserve(run, open, filters, pool, rule, matches: true, wantMatching, dayOf);
            if (matching.Count < wantMatching && rule.Match == RecipeMatchKind.Favourite)
            {
                run.Warnings.Add($"Only {matching.Count + keptMatching} of {count} {RecipeMatcher.Plural(slot)} could be someone's favourite without repeating a recent meal. Add more favourites, or change the plan style.");
            }
            else if (matching.Count < wantMatching)
            {
                run.Warnings.Add(matching.Count + Math.Min(keptMatching, count) == 0
                    ? $"No {RecipeMatcher.Plural(slot)} could be {RecipeMatcher.Describe(rule, run.Request.CookbookNames)}, as there are no such recipes yet. Add some, or change the plan style."
                    : $"Only {matching.Count + keptMatching} of {count} {RecipeMatcher.Plural(slot)} could be {RecipeMatcher.Describe(rule, run.Request.CookbookNames)}, as there aren't enough such recipes. Add some, or change the plan style.");
            }

            var rest = open.Where(key => !matching.Contains(key)).ToList();
            var other = Reserve(run, rest, filters, pool, rule, matches: false, rule.Limit == RuleLimit.Exactly ? rest.Count : wantOther, dayOf);
            if (other.Count < Math.Min(wantOther, rest.Count))
            {
                run.Warnings.Add($"There weren't enough recipes that aren't {RecipeMatcher.Describe(rule, run.Request.CookbookNames)}, so some {RecipeMatcher.Plural(slot)} are. Add some, or change the plan style.");
            }
        }

        while (true)
        {
            var picks = WeekSelector.Choose(open, Pools(run, open, filters, pool, rules), run.Chosen, run.Scorer.Score, run.Random);
            if (picks is not null)
            {
                return picks;
            }

            // Relax the last rule still narrowing a slot, then try again.
            var relaxed = rules.LastOrDefault(rule => filters.Values.Any(list => list.Any(filter => filter.Rule == rule)));
            if (relaxed is null)
            {
                return null;
            }

            foreach (var list in filters.Values)
            {
                list.RemoveAll(filter => filter.Rule == relaxed);
            }

            run.Warnings.Add($"The rule for {RecipeMatcher.Plural(slot)} {RecipeMatcher.Describe(relaxed, run.Request.CookbookNames)} couldn't be met with the recipes there are, so it was relaxed this week.");
        }
    }

    /// <summary>
    /// Narrows up to <paramref name="count"/> slots to recipes that match the rule (or don't), choosing slots that can
    /// still be filled: the rule's day first, then the least narrowed. Never more than there are such recipes.
    /// </summary>
    /// <remarks>
    /// Favourites planned recently don't count, so a rule asking for favourites is relaxed before a recent meal is
    /// repeated to meet it.
    /// </remarks>
    private static List<TSlot> Reserve<TSlot>(
        Run run,
        IEnumerable<TSlot> candidates, Dictionary<TSlot, List<Filter>> filters, IReadOnlyList<Recipe> pool,
        PlanRule rule, bool matches, int count, Func<TSlot, DayOfWeek?> dayOf)
        where TSlot : notnull
    {
        var filter = new Filter(rule, matches, run.Request.Fans);
        var eligible = matches && rule.Match == RecipeMatchKind.Favourite
            ? pool.Where(recipe => !run.Request.RecentRecipeIds.Contains(recipe.Id)).ToList()
            : pool;
        count = Math.Min(count, eligible.Count(filter.Allows));
        if (count <= 0)
        {
            return [];
        }

        var chosen = candidates
            .Where(key => eligible.Any(recipe => filter.Allows(recipe) && filters[key].All(other => other.Allows(recipe))))
            .OrderBy(key => matches && rule.Day is not null && dayOf(key) == rule.Day ? 0 : 1)
            .ThenBy(key => filters[key].Count)
            .Take(count)
            .ToList();

        foreach (var key in chosen)
        {
            filters[key].Add(filter);
        }

        return chosen;
    }

    /// <summary>The recipes each slot may choose from: the pool narrowed by its rules, recent recipes left out where possible.</summary>
    private static Dictionary<TSlot, IReadOnlyList<Recipe>> Pools<TSlot>(
        Run run, IReadOnlyList<TSlot> open, Dictionary<TSlot, List<Filter>> filters, IReadOnlyList<Recipe> pool, List<PlanRule> rules)
        where TSlot : notnull
    {
        // Slots narrowed by the same rules share one pool, so recent recipes are only left out when enough others remain for all of them.
        var pools = new Dictionary<TSlot, IReadOnlyList<Recipe>>();
        foreach (var group in open.GroupBy(key => string.Join(";", filters[key].Select(filter => $"{rules.IndexOf(filter.Rule)}{(filter.Matches ? '+' : '-')}").Order())))
        {
            var allowed = pool.Where(recipe => filters[group.First()].All(filter => filter.Allows(recipe))).ToList();
            var fresh = PreferFresh(run, allowed, group.Count());
            foreach (var key in group)
            {
                pools[key] = fresh;
            }
        }

        return pools;
    }

    private static List<Recipe> KeptRecipes(IEnumerable<PlannedMeal> kept) =>
        kept.Where(meal => meal.Recipe is not null).Select(meal => meal.Recipe!).DistinctBy(recipe => recipe.Id).ToList();

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

    /// <summary>One rule narrowing a slot: to recipes that match it, or to recipes that don't.</summary>
    private sealed record Filter(PlanRule Rule, bool Matches, IReadOnlyDictionary<Guid, int>? Fans)
    {
        public bool Allows(Recipe recipe) => RecipeMatcher.Matches(recipe, Rule, Fans) == Matches;
    }

    /// <summary>What a favourite recipe adds to a week's score, for the style's choice.</summary>
    private double FavouriteWeight(FavouriteLean lean) => lean switch
    {
        FavouriteLean.ALittle => options.Weights.Favourite,
        FavouriteLean.ALot => options.Weights.FavouriteALot,
        _ => 0,
    };

    /// <summary>The state of one planning pass.</summary>
    private sealed record Run(
        PlanRequest Request,
        PlanStyle Style,
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
