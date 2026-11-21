using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Keeping plan styles, staging and shuffling weeks with them, and listing the open weeks.</summary>
public class PlanStyleManagerTests
{
    private static readonly DateTimeOffset SaturdayTen = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly Recipe Lemonade = TestData.NewRecipe("Lemonade", RecipeCourse.Drink, false, "4 lemons");

    private static TestCore WithLibrary()
    {
        var core = new TestCore();
        core.Clock.Now = SaturdayTen;
        core.Recipes.Rows.AddRange(TestData.Library);
        core.Recipes.Rows.Add(Lemonade);
        core.Recipes.Rows[0].Tags = ["Comfort", "comfort", "Winter"];
        return core;
    }

    private static PlanStyleDetails DrinksOnly(string name = "Drinks only", bool isDefault = false) =>
        new(null, name, "Just a drink.", isDefault, [], false, 0, 0, 0, 1, []);

    [Test]
    public async Task With_no_styles_saved_the_starter_styles_are_offered_and_used()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();

        var styles = await plans.GetStylesAsync(default);
        var plan = await plans.StageAsync(new StagePlanRequest(), default);

        await Assert.That(styles.Styles).IsEmpty();
        await Assert.That(styles.Known.Single().Name).IsEqualTo("Classic week");
        await Assert.That(styles.Known.Single().Id).IsNull();
        await Assert.That(styles.Tags).IsEquivalentTo(["Comfort", "Winter"]);
        await Assert.That(styles.Ingredients).Contains("cabbage");
        await Assert.That(plan.StyleName).IsEqualTo("Classic week");
        await Assert.That(plan.Meals.Count(meal => meal.Slot == MealSlot.Dinner)).IsEqualTo(4);
    }

    [Test]
    public async Task Saved_styles_keep_their_ids_and_the_default_is_used_for_staging()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();

        var saved = await plans.SaveStylesAsync(new SavePlanStylesRequest([DrinksOnly(isDefault: true), DrinksOnly("Spare")]), default);
        var again = await plans.SaveStylesAsync(new SavePlanStylesRequest([saved.Styles[0] with { Description = "Changed" }]), default);
        var plan = await plans.StageAsync(new StagePlanRequest(), default);

        await Assert.That(again.Styles.Single().Id).IsEqualTo(saved.Styles[0].Id);
        await Assert.That(core.Settings.Styles.Single().Description).IsEqualTo("Changed");
        await Assert.That(plan.StyleName).IsEqualTo("Drinks only");
        await Assert.That(plan.Meals.Single().Slot).IsEqualTo(MealSlot.Drink);
        await Assert.That(plan.Meals.Single().RecipeId).IsEqualTo(Lemonade.Id);
    }

    [Test]
    public async Task Staging_with_a_chosen_style_uses_it_and_replanning_keeps_it()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var saved = await plans.SaveStylesAsync(new SavePlanStylesRequest([DrinksOnly("Usual", isDefault: true) with { CookingDays = [DayOfWeek.Sunday] }, DrinksOnly()]), default);

        var plan = await plans.StageAsync(new StagePlanRequest(StyleId: saved.Styles[1].Id), default);
        var regenerated = await plans.RegenerateFromPantryAsync(plan.Id, default);

        await Assert.That(plan.StyleName).IsEqualTo("Drinks only");
        await Assert.That(regenerated.StyleName).IsEqualTo("Drinks only");
        await Assert.That(regenerated.Meals.Single().Slot).IsEqualTo(MealSlot.Drink);
        await Assert.That(await TestData.Caught<NotFoundException>(() => plans.StageAsync(new StagePlanRequest(StyleId: Guid.NewGuid()), default))).IsNotNull();
    }

    [Test]
    public async Task Shuffling_keeps_the_meals_asked_for()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var first = await plans.StageAsync(new StagePlanRequest(), default);
        var kept = first.Meals.Where(meal => meal.Slot == MealSlot.Dinner).Take(2).ToList();

        var shuffled = await plans.StageAsync(new StagePlanRequest(first.WeekStart, Keep: kept.Select(meal => meal.Id).ToList()), default);

        foreach (var meal in kept)
        {
            await Assert.That(shuffled.Meals.Count(again => again.Slot == MealSlot.Dinner && again.Date == meal.Date && again.RecipeId == meal.RecipeId)).IsEqualTo(1);
        }

        await Assert.That(shuffled.Meals.Count(meal => meal.Slot == MealSlot.Dinner)).IsEqualTo(4);
    }

    [Test]
    public async Task Open_plans_list_this_week_and_a_staged_next_week()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var next = await plans.StageAsync(new StagePlanRequest(), default);
        var later = await plans.StageAsync(new StagePlanRequest(next.WeekStart.AddDays(7)), default);
        await plans.ConfirmAsync(next.Id, default);

        var open = await plans.ListOpenAsync(default);

        await Assert.That(open.Select(plan => plan.Id)).IsEquivalentTo([next.Id, later.Id]);
        await Assert.That(open[0].Status).IsEqualTo(PlanStatus.Confirmed);
        await Assert.That(open[1].StyleName).IsEqualTo("Classic week");
    }

    [Test]
    public async Task Bad_styles_are_refused_with_a_plain_reason()
    {
        var plans = WithLibrary().PlanManager();

        var problem = await TestData.Caught<RuleViolationException>(() =>
            plans.SaveStylesAsync(new SavePlanStylesRequest([DrinksOnly(), DrinksOnly()]), default));

        await Assert.That(problem!.Message).IsEqualTo("There are two plan styles called Drinks only. Give each one its own name.");
    }
}
