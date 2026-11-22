using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Adding a recipe to a plan from its page, for a chosen number of servings.</summary>
public class AddMealTests
{
    /// <summary>A Saturday morning; the next plan's week starts the day after.</summary>
    private static readonly DateTimeOffset SaturdayTen = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly NextSunday = new(2026, 11, 15);

    private static readonly DateOnly Tuesday = NextSunday.AddDays(2);

    private static TestCore WithLibrary()
    {
        var core = new TestCore();
        core.Clock.Now = SaturdayTen;
        core.Recipes.Rows.AddRange(TestData.Library);
        return core;
    }

    private static AddMealRequest TacosOn(DateOnly? date, MealSlot slot = MealSlot.Dinner, int? servings = null, Guid? replaces = null) =>
        new(TestData.Tacos.Id, NextSunday, date, slot, servings, replaces);

    [Test]
    public async Task A_week_with_no_plan_gets_a_staged_one_holding_just_the_meal()
    {
        var core = WithLibrary();

        var plan = await core.PlanManager().AddMealAsync(TacosOn(Tuesday, servings: 6), default);

        await Assert.That(plan.Status).IsEqualTo(PlanStatus.Staged);
        await Assert.That(plan.WeekStart).IsEqualTo(NextSunday);
        var meal = plan.Meals.Single();
        await Assert.That(meal.Title).IsEqualTo(TestData.Tacos.Name);
        await Assert.That(meal.Date).IsEqualTo(Tuesday);
        await Assert.That(meal.Portions).IsEqualTo(6);
        await Assert.That(meal.PortionsChosen).IsTrue();
        await Assert.That(core.Messages.Published.OfType<MealAdded>().Single()).IsEqualTo(new MealAdded(plan.Id, meal.Id));
    }

    [Test]
    public async Task The_shopping_list_catches_up_when_the_event_is_handled_and_leaves_out_what_the_pantry_has()
    {
        var core = WithLibrary();
        core.Pantry.Rows.Add(TestData.Stocked("tortillas", 4m));

        var added = await core.PlanManager().AddMealAsync(TacosOn(Tuesday, servings: 6), default);
        await Assert.That(added.Shopping).IsEmpty().Because("the list is left to the MealAdded handler");

        await core.HandleEventsAsync();

        var plan = await core.PlanManager().GetAsync(added.Id, default);
        // Six servings of a recipe for four is one and a half batches.
        await Assert.That(plan.Shopping.Single(line => line.Name == "cabbage").Quantity).IsEqualTo(0.75m);
        await Assert.That(plan.Shopping.Single(line => line.Name.StartsWith("tortilla", StringComparison.Ordinal)).Quantity).IsEqualTo(8m);
        await Assert.That(core.Messages.Published.OfType<PlanChanged>().Any(changed => changed.PlanId == plan.Id)).IsTrue();
    }

    [Test]
    public async Task Without_servings_it_is_made_for_the_plans_people()
    {
        var core = WithLibrary();
        var staged = await core.PlanManager().StageAsync(new StagePlanRequest(), default);

        var plan = await core.PlanManager().AddMealAsync(TacosOn(Tuesday, MealSlot.Lunch), default);

        var meal = plan.Meals.Single(meal => meal is { Slot: MealSlot.Lunch, Date: var date } && date == Tuesday && !meal.IsLeftover);
        await Assert.That(plan.Id).IsEqualTo(staged.Id);
        await Assert.That(meal.Portions).IsEqualTo(core.Plans.Rows.Single().People);
        await Assert.That(meal.PortionsChosen).IsFalse();
    }

    [Test]
    public async Task Replacing_swaps_the_recipe_into_the_chosen_meal()
    {
        var core = WithLibrary();
        var staged = await core.PlanManager().StageAsync(new StagePlanRequest(), default);
        var dinner = staged.Meals.First(meal => meal is { Slot: MealSlot.Dinner, Date: not null });
        var mealCount = staged.Meals.Count(meal => !meal.IsLeftover);

        var plan = await core.PlanManager().AddMealAsync(TacosOn(dinner.Date, servings: 3, replaces: dinner.Id), default);
        await core.HandleEventsAsync();

        var swapped = plan.Meals.Single(meal => meal.Id == dinner.Id);
        await Assert.That(swapped.RecipeId).IsEqualTo(TestData.Tacos.Id);
        await Assert.That(swapped.Portions).IsEqualTo(3);
        await Assert.That(plan.Meals.Count(meal => !meal.IsLeftover)).IsEqualTo(mealCount);
    }

    [Test]
    public async Task Replacing_a_meal_that_has_moved_since_is_refused()
    {
        var core = WithLibrary();
        var staged = await core.PlanManager().StageAsync(new StagePlanRequest(), default);
        var dinner = staged.Meals.First(meal => meal is { Slot: MealSlot.Dinner, Date: not null });

        var otherDay = dinner.Date == Tuesday ? Tuesday.AddDays(1) : Tuesday;

        var refused = await TestData.Caught<ConflictException>(() =>
            core.PlanManager().AddMealAsync(TacosOn(otherDay, replaces: dinner.Id), default));

        await Assert.That(refused).IsNotNull();
    }

    [Test]
    public async Task Requests_that_do_not_fit_the_week_are_refused_with_a_reason()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();

        await Assert.That((await TestData.Caught<RuleViolationException>(() => plans.AddMealAsync(TacosOn(NextSunday.AddDays(7)), default)))?.Message)
            .IsEqualTo("Choose a day inside the plan's week.");
        await Assert.That((await TestData.Caught<RuleViolationException>(() => plans.AddMealAsync(TacosOn(null), default)))?.Message)
            .IsEqualTo("Choose a day for this dinner.");
        await Assert.That((await TestData.Caught<RuleViolationException>(() => plans.AddMealAsync(TacosOn(Tuesday, servings: 0), default)))?.Message)
            .IsEqualTo("Make it for between 1 and 50 servings.");
        await Assert.That(await TestData.Caught<RuleViolationException>(() =>
            plans.AddMealAsync(TacosOn(Tuesday) with { WeekStart = NextSunday.AddDays(1) }, default))).IsNotNull();

        // A snack can be made on any day.
        var snack = await plans.AddMealAsync(TacosOn(null, MealSlot.Snack), default);
        await Assert.That(snack.Meals.Single().Date).IsNull();
    }

    [Test]
    public async Task An_archived_recipe_cannot_be_added()
    {
        var core = new TestCore();
        core.Clock.Now = SaturdayTen;
        var archived = TestData.NewRecipe("Old Favourite", RecipeCourse.Main, false, "1 bunch kale");
        archived.IsArchived = true;
        core.Recipes.Rows.Add(archived);

        var refused = await TestData.Caught<RuleViolationException>(() =>
            core.PlanManager().AddMealAsync(new AddMealRequest(archived.Id, NextSunday, Tuesday, MealSlot.Dinner), default));

        await Assert.That(refused).IsNotNull();
        await Assert.That(core.Plans.Rows).IsEmpty();
    }

    [Test]
    public async Task Chosen_servings_survive_resizing_and_leftover_lunches()
    {
        var plan = TestData.PlanWeek(NextSunday).Plan;
        var dinner = TestData.Dinners(plan).First();
        PlanningEngine.ChoosePortions(dinner, 7);

        PlanningEngine.Resize(plan, 3);
        PlanningEngine.PlaceLeftovers(plan, maxLeftoverDays: 3);

        await Assert.That(dinner.Portions).IsEqualTo(7);
        await Assert.That(plan.Meals.Any(meal => meal.LeftoverOfMealId == dinner.Id)).IsFalse();
        await Assert.That(TestData.Dinners(plan).Where(other => other != dinner).All(other => other.Portions >= 3)).IsTrue();
    }

    [Test]
    public async Task Catching_up_a_plan_that_was_cleared_does_nothing()
    {
        var core = WithLibrary();
        var plan = await core.PlanManager().AddMealAsync(TacosOn(Tuesday), default);
        await core.PlanManager().ClearAsync(plan.Id, default);

        await Assert.That(await core.PlanManager().CatchUpAsync(plan.Id, default)).IsFalse();
        await Assert.That(core.Plans.Rows).IsEmpty();
    }

    [Test]
    public async Task Staging_the_week_again_keeps_the_chosen_servings_of_kept_meals()
    {
        var core = WithLibrary();
        var added = await core.PlanManager().AddMealAsync(TacosOn(Tuesday, servings: 5), default);
        var tacos = added.Meals.Single();

        var restaged = await core.PlanManager().StageAsync(new StagePlanRequest(NextSunday, Keep: [tacos.Id]), default);

        var kept = restaged.Meals.Single(meal => meal.RecipeId == TestData.Tacos.Id && meal.Date == Tuesday);
        await Assert.That(kept.Portions).IsEqualTo(5);
        await Assert.That(kept.PortionsChosen).IsTrue();
    }
}
