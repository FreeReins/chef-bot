using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Planning a week in a plan style: its days and counts, and its rules about recipes.</summary>
public class PlanStyleTests
{
    private static readonly Recipe Chili = TestData.NewRecipe("Slow Cooker Chili", RecipeCourse.Main, true, "1 lb beef", "1 can canned tomatoes");
    private static readonly Recipe Curry = TestData.NewRecipe("Slow Cooker Curry", RecipeCourse.Main, true, "1 lb chicken thigh", "2 cups brown rice");
    private static readonly Recipe Lemonade = TestData.NewRecipe("Lemonade", RecipeCourse.Drink, false, "4 lemons");
    private static readonly Recipe Smoothie = TestData.NewRecipe("Apple Smoothie", RecipeCourse.Drink, false, "2 apples");

    private static readonly IReadOnlyList<Recipe> WithSlowCookers = [.. TestData.Library, Chili, Curry];

    private static PlanStyle Style(params PlanRule[] rules)
    {
        var style = PlanStyle.Classic();
        style.Rules = [.. rules];
        return style;
    }

    private static PlanRule Rule(RuleLimit limit, int count, RecipeMatchKind match, MealSlot? slot = MealSlot.Dinner, params string[] values) =>
        new() { Limit = limit, Count = count, Match = match, Slot = slot, Values = [.. values] };

    private static PlanDraft Plan(PlanStyle style, IReadOnlyList<Recipe>? recipes = null, IReadOnlyList<PlannedMeal>? kept = null, int? seed = null) =>
        TestData.Planning.Plan(new PlanRequest(
            TestData.Sunday, recipes ?? WithSlowCookers, new HashSet<Guid>(), PantrySnapshot.Empty, DateTimeOffset.UnixEpoch,
            Seed: seed, Style: style, Kept: kept));

    [Test]
    public async Task At_least_three_slow_cooker_dinners_makes_every_dinner_slow_cooker_on_three_days()
    {
        var style = Style(Rule(RuleLimit.AtLeast, 3, RecipeMatchKind.SlowCooker));
        style.CookingDays = [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday];

        var dinners = TestData.Dinners(Plan(style).Plan);

        await Assert.That(dinners.Count).IsEqualTo(3);
        await Assert.That(dinners.All(dinner => dinner.Recipe!.IsSlowCooker)).IsTrue();
    }

    [Test]
    public async Task No_slow_cooker_meals_keeps_them_all_out()
    {
        var draft = Plan(Style(Rule(RuleLimit.None, 0, RecipeMatchKind.SlowCooker, slot: null)));

        await Assert.That(TestData.Dinners(draft.Plan).Count).IsEqualTo(4);
        await Assert.That(TestData.Dinners(draft.Plan).Any(dinner => dinner.Recipe!.IsSlowCooker)).IsFalse();
        await Assert.That(draft.Warnings.Any(warning => warning.Contains("slow cooker"))).IsFalse();
    }

    [Test]
    public async Task Exactly_one_slow_cooker_dinner_goes_on_its_day_and_no_other()
    {
        var rule = Rule(RuleLimit.Exactly, 1, RecipeMatchKind.SlowCooker);
        rule.Day = DayOfWeek.Thursday;

        foreach (var seed in Enumerable.Range(1, 5))
        {
            var dinners = TestData.Dinners(Plan(Style(rule), seed: seed).Plan);

            await Assert.That(dinners.Count(dinner => dinner.Recipe!.IsSlowCooker)).IsEqualTo(1);
            await Assert.That(dinners.Single(dinner => dinner.Recipe!.IsSlowCooker).Date!.Value.DayOfWeek).IsEqualTo(DayOfWeek.Thursday);
        }
    }

    [Test]
    public async Task An_ingredient_target_plans_dinners_with_that_ingredient()
    {
        var dinners = TestData.Dinners(Plan(Style(Rule(RuleLimit.AtLeast, 2, RecipeMatchKind.Ingredient, values: "cabbage")), TestData.Library).Plan);

        await Assert.That(dinners.Select(dinner => dinner.RecipeId)).Contains(TestData.Tacos.Id);
        await Assert.That(dinners.Select(dinner => dinner.RecipeId)).Contains(TestData.Bowls.Id);
    }

    [Test]
    public async Task Leaving_out_an_ingredient_also_leaves_out_its_kinds_and_broths()
    {
        var dinners = TestData.Dinners(Plan(Style(Rule(RuleLimit.None, 0, RecipeMatchKind.Ingredient, slot: null, "chicken")), TestData.Library).Plan);

        await Assert.That(dinners.Count).IsEqualTo(4);
        await Assert.That(dinners.Any(dinner => dinner.RecipeId == TestData.Bowls.Id || dinner.RecipeId == TestData.Soup.Id)).IsFalse();
    }

    [Test]
    public async Task Quick_and_tagged_rules_choose_recipes_by_their_time_and_tags()
    {
        var quick = TestData.NewRecipe("Quick Kale Salad", RecipeCourse.Main, false, "1 bunch kale");
        quick.TotalMinutes = 15;
        var favorite = TestData.NewRecipe("Family Tacos", RecipeCourse.Main, false, "8 tortillas");
        favorite.Tags = ["Family Favorite"];
        favorite.PrepMinutes = 10;
        favorite.CookMinutes = 5;
        var quickRule = Rule(RuleLimit.AtLeast, 2, RecipeMatchKind.ReadyWithin);
        quickRule.Minutes = 20;

        var quickDinners = TestData.Dinners(Plan(Style(quickRule), [.. TestData.Library, quick, favorite]).Plan);
        var tagged = TestData.Dinners(Plan(Style(Rule(RuleLimit.AtLeast, 1, RecipeMatchKind.Tag, values: "family favorite")), [.. TestData.Library, favorite]).Plan);

        await Assert.That(quickDinners.Select(dinner => dinner.RecipeId)).Contains(quick.Id);
        await Assert.That(quickDinners.Select(dinner => dinner.RecipeId)).Contains(favorite.Id);
        await Assert.That(tagged.Select(dinner => dinner.RecipeId)).Contains(favorite.Id);
    }

    [Test]
    public async Task Drinks_are_planned_on_any_day_like_snacks()
    {
        var style = Style();
        style.Drinks = 2;

        var drinks = Plan(style, [.. TestData.Library, Lemonade, Smoothie]).Plan.Meals.Where(meal => meal.Slot == MealSlot.Drink).ToList();

        await Assert.That(drinks.Count).IsEqualTo(2);
        await Assert.That(drinks.All(drink => drink.Date is null && drink.Portions == 0)).IsTrue();
    }

    [Test]
    public async Task Cooking_days_and_leftover_lunches_come_from_the_style()
    {
        var style = Style();
        style.CookingDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday];
        style.LeftoverLunches = false;
        style.Breakfasts = 0;
        style.Snacks = 0;
        style.Sweets = 0;

        var plan = Plan(style).Plan;
        var dinners = TestData.Dinners(plan);

        await Assert.That(string.Join(",", dinners.Select(dinner => dinner.Date!.Value.DayOfWeek))).IsEqualTo("Monday,Wednesday,Friday");
        await Assert.That(plan.Meals.Any(meal => meal.Slot is MealSlot.Lunch or MealSlot.Breakfast or MealSlot.Snack or MealSlot.Sweet)).IsFalse();
        await Assert.That(dinners.All(dinner => dinner.Portions == PlannerSettings.DefaultPeople)).IsTrue();
        await Assert.That(plan.LeftoverLunches).IsFalse();
        await Assert.That(plan.StyleName).IsEqualTo(style.Name);
    }

    [Test]
    public async Task A_style_without_dinners_plans_the_rest_even_with_no_main_courses()
    {
        var style = Style();
        style.CookingDays = [];

        var plan = Plan(style, TestData.Library.Where(recipe => recipe.Course != RecipeCourse.Main).ToList()).Plan;

        await Assert.That(plan.Meals.Any(meal => meal.Slot == MealSlot.Dinner)).IsFalse();
        await Assert.That(plan.Meals.Count(meal => meal.Slot == MealSlot.Snack)).IsEqualTo(3);
    }

    [Test]
    public async Task A_rule_that_cant_be_met_is_relaxed_with_a_note_instead_of_failing()
    {
        var draft = Plan(Style(Rule(RuleLimit.AtLeast, 3, RecipeMatchKind.SlowCooker)), TestData.Library);

        await Assert.That(TestData.Dinners(draft.Plan).Count).IsEqualTo(4);
        await Assert.That(draft.Warnings.Any(warning => warning.StartsWith("Only 1 of 3 dinners could be slow cooker"))).IsTrue();
    }

    [Test]
    public async Task Kept_meals_stay_as_they_are_and_the_rest_is_planned_around_them()
    {
        var first = Plan(Style(), seed: 1).Plan;
        var tuesday = TestData.Dinners(first).Single(dinner => dinner.Date!.Value.DayOfWeek == DayOfWeek.Tuesday);
        var snack = first.Meals.First(meal => meal.Slot == MealSlot.Snack);

        foreach (var seed in Enumerable.Range(2, 4))
        {
            var again = Plan(Style(), kept: [tuesday, snack], seed: seed).Plan;
            var dinners = TestData.Dinners(again);

            await Assert.That(dinners.Count).IsEqualTo(4);
            await Assert.That(dinners.Single(dinner => dinner.Date == tuesday.Date).RecipeId).IsEqualTo(tuesday.RecipeId);
            await Assert.That(dinners.Select(dinner => dinner.RecipeId).Distinct().Count()).IsEqualTo(4);
            await Assert.That(again.Meals.Count(meal => meal.Slot == MealSlot.Snack && meal.RecipeId == snack.RecipeId)).IsEqualTo(1);
            await Assert.That(again.Meals.Count(meal => meal.Slot == MealSlot.Snack)).IsEqualTo(3);
        }
    }

    [Test]
    public async Task A_kept_dinner_moved_off_the_cooking_days_stands_in_for_the_nearest_one()
    {
        var first = Plan(Style(), seed: 1).Plan;
        var moved = TestData.Dinners(first)[0];
        moved.Date = TestData.Sunday.AddDays((int)DayOfWeek.Wednesday);

        var dinners = TestData.Dinners(Plan(Style(), kept: [moved], seed: 2).Plan);

        await Assert.That(dinners.Count).IsEqualTo(4);
        await Assert.That(dinners.Select(dinner => dinner.Date!.Value.DayOfWeek)).Contains(DayOfWeek.Wednesday);
    }

    [Test]
    public async Task Moving_a_dinner_onto_another_swaps_their_days()
    {
        var plan = Plan(Style(), seed: 1).Plan;
        var sunday = TestData.Dinners(plan)[0];
        var tuesday = TestData.Dinners(plan)[1];
        var tuesdayDate = tuesday.Date!.Value;

        var swapped = PlanningEngine.Move(plan, sunday, tuesdayDate);

        await Assert.That(swapped).IsEqualTo(tuesday);
        await Assert.That(sunday.Date).IsEqualTo(tuesdayDate);
        await Assert.That(tuesday.Date).IsEqualTo(TestData.Sunday);
    }

    [Test]
    public async Task Every_shipped_starter_style_is_valid_and_the_first_is_the_classic_week()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config", "plan-styles.json");
        var styles = KnownPlanStyleAccess.FromJson(File.ReadAllText(path));

        var checkedStyles = ValidationEngine.PlanStyles(styles.Select(style => Managers.Mapping.ToResponse(style, saved: false)).ToList());

        await Assert.That(checkedStyles.Count).IsEqualTo(styles.Count);
        await Assert.That(styles.Count >= 5).IsTrue();
        await Assert.That(styles[0].Name).IsEqualTo(PlanStyle.Classic().Name);
        await Assert.That(styles[0].IsDefault).IsTrue();
        await Assert.That(styles.Skip(1).Any(style => style.IsDefault)).IsFalse();
        await Assert.That(styles.All(style => style.Description.Length > 0)).IsTrue();
    }

    [Test]
    public async Task Style_rules_are_checked_before_saving()
    {
        PlanStyleDetails Valid(string name, params PlanRuleDetails[] rules) =>
            new(null, name, null, false, [DayOfWeek.Sunday], true, 0, 0, 0, 0, rules);

        var defaulted = ValidationEngine.PlanStyles([Valid("A"), Valid("B")]);

        await Assert.That(defaulted[0].IsDefault).IsTrue();
        await Assert.That(Refused([Valid("A"), Valid("a")])).IsTrue();
        await Assert.That(Refused([Valid("A", new PlanRuleDetails(RuleLimit.AtLeast, 1, MealSlot.Dinner, RecipeMatchKind.Tag, []))])).IsTrue();
        await Assert.That(Refused([Valid("A", new PlanRuleDetails(RuleLimit.AtLeast, 1, null, RecipeMatchKind.SlowCooker))])).IsTrue();
        await Assert.That(Refused([Valid("A", new PlanRuleDetails(RuleLimit.AtLeast, 1, MealSlot.Snack, RecipeMatchKind.SlowCooker, Day: DayOfWeek.Monday))])).IsTrue();
        await Assert.That(Refused([Valid("A", new PlanRuleDetails(RuleLimit.AtLeast, 1, MealSlot.Dinner, RecipeMatchKind.ReadyWithin))])).IsTrue();
        await Assert.That(Refused([new PlanStyleDetails(null, "Empty", null, true, [], true, 0, 0, 0, 0, [])])).IsTrue();
        await Assert.That(Refused([new PlanStyleDetails(null, "Twice", null, true, [DayOfWeek.Monday, DayOfWeek.Monday], true, 0, 0, 0, 0, [])])).IsTrue();
    }

    private static bool Refused(IReadOnlyList<PlanStyleDetails> styles)
    {
        try
        {
            ValidationEngine.PlanStyles(styles);
            return false;
        }
        catch (RuleViolationException)
        {
            return true;
        }
    }
}
