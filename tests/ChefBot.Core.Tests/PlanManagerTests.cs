using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

public class PlanManagerTests
{
    /// <summary>A Saturday morning, after the default schedule time (Saturday 09:00).</summary>
    private static readonly DateTimeOffset SaturdayTen = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly NextSunday = new(2026, 11, 15);

    private static TestCore WithLibrary(PlannerOptions? options = null)
    {
        var core = new TestCore(options: options);
        core.Clock.Now = SaturdayTen;
        core.Recipes.Rows.AddRange(TestData.Library);
        return core;
    }

    [Test]
    public async Task Staging_saves_the_plan_with_its_shopping_list_and_announces_it()
    {
        var core = WithLibrary();

        var plan = await core.PlanManager().StageAsync(new StagePlanRequest(), default);

        await Assert.That(plan.WeekStart).IsEqualTo(NextSunday);
        await Assert.That(plan.Shopping.Count > 0).IsTrue();
        await Assert.That(core.Plans.Rows.Single().ShoppingItems.Count).IsEqualTo(plan.Shopping.Count);
        await Assert.That(core.Messages.Published.OfType<PlanStaged>().Single()).IsEqualTo(new PlanStaged(plan.Id, "manual"));
    }

    [Test]
    public async Task Staging_a_week_again_replaces_the_staged_plan_but_not_a_confirmed_one()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var first = await plans.StageAsync(new StagePlanRequest(), default);

        var second = await plans.StageAsync(new StagePlanRequest(), default);
        await Assert.That(core.Plans.Rows.Single().Id).IsEqualTo(second.Id);

        await plans.ConfirmAsync(second.Id, default);
        await Assert.That(await TestData.Caught<ConflictException>(() => plans.StageAsync(new StagePlanRequest(), default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.StageAsync(new StagePlanRequest(NextSunday.AddDays(1)), default))).IsNotNull();
        await Assert.That(first.Id == second.Id).IsFalse();
    }

    [Test]
    public async Task Editing_the_pantry_announces_it_and_the_open_plans_follow()
    {
        var core = WithLibrary();
        var plan = await core.PlanManager().StageAsync(new StagePlanRequest(), default);
        var item = plan.Shopping.First();

        await core.PantryManager().AddAsync(new NewPantryItem(item.Name, 100, item.Unit), default);
        await Assert.That(core.Messages.Published.OfType<PantryChanged>().Count()).IsEqualTo(1);

        // What the PantryChanged handler does: the item now on hand drops off the list.
        await core.PlanManager().RefreshOpenPlansAsync(default);
        var refreshed = await core.PlanManager().GetAsync(plan.Id, default);
        await Assert.That(refreshed.Shopping.Any(line => line.Name == item.Name)).IsFalse();
    }

    [Test]
    public async Task Purchases_need_a_confirmed_plan_and_go_into_the_pantry()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        var item = plan.Shopping.First();

        await Assert.That(await TestData.Caught<ConflictException>(() => plans.PurchaseAsync(plan.Id, item.Id, new PurchaseDetails(), default))).IsNotNull();

        await plans.ConfirmAsync(plan.Id, default);
        var after = await plans.PurchaseAsync(plan.Id, item.Id, new PurchaseDetails(), default);

        await Assert.That(after.Shopping.Single(line => line.Id == item.Id).IsPurchased).IsTrue();
        await Assert.That(core.Pantry.Rows.Single().DisplayName).IsEqualTo(item.Name);
    }

    [Test]
    public async Task Cooking_a_meal_uses_up_the_pantry()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        var stew = plan.Meals.Single(meal => meal.Title == TestData.Stew.Name && !meal.IsLeftover);
        core.Pantry.Rows.Add(TestData.Stocked("stew meat", 10, "lb"));

        await plans.SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Cooked), default);

        await Assert.That(core.Pantry.Rows.Single().Quantity < 10m).IsTrue();
    }

    [Test]
    public async Task The_weekly_schedule_stages_the_coming_week_once()
    {
        var core = WithLibrary();

        await core.PlanManager().CheckWeeklyScheduleAsync(default);
        await core.PlanManager().CheckWeeklyScheduleAsync(default);

        await Assert.That(core.Plans.Rows.Single().WeekStart).IsEqualTo(NextSunday);
        await Assert.That(core.Settings.Schedule.LastStagedWeek).IsEqualTo(NextSunday);
        await Assert.That(core.Messages.Published.OfType<PlanStaged>().Single().Trigger).IsEqualTo("schedule");
    }

    [Test]
    public async Task A_week_cleared_after_the_schedule_staged_it_is_not_staged_again()
    {
        var core = WithLibrary();
        await core.PlanManager().CheckWeeklyScheduleAsync(default);
        await core.PlanManager().ClearAsync(core.Plans.Rows.Single().Id, default);

        await core.PlanManager().CheckWeeklyScheduleAsync(default);

        await Assert.That(core.Plans.Rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task With_import_first_the_schedule_asks_for_a_Mealie_import_and_stages_after_it()
    {
        var core = WithLibrary(new PlannerOptions { ImportFromMealieBeforePlanning = true });
        core.Settings.Mealie = new MealieSettings { Url = "http://mealie.local", ApiToken = "token" };

        await core.PlanManager().CheckWeeklyScheduleAsync(default);

        await Assert.That(core.Messages.Published.OfType<ImportMealieRecipes>().Single().StageWeekAfter).IsEqualTo(NextSunday);
        await Assert.That(core.Plans.Rows.Count).IsEqualTo(0);

        // What follows RecipeImportFinished, whether the import worked or not.
        await core.PlanManager().StageScheduledWeekAsync(new StageScheduledWeek(NextSunday), default);
        await Assert.That(core.Plans.Rows.Single().WeekStart).IsEqualTo(NextSunday);
    }

    [Test]
    public async Task The_schedule_is_due_from_its_time_until_the_week_starts()
    {
        var planning = TestData.Planning;

        await Assert.That(planning.ScheduledWeekDue(SaturdayTen.AddHours(-2), null) is null).IsTrue();
        await Assert.That(planning.ScheduledWeekDue(SaturdayTen, null)).IsEqualTo(NextSunday);
        await Assert.That(planning.ScheduledWeekDue(SaturdayTen.AddDays(1), null)).IsEqualTo(NextSunday);
        await Assert.That(planning.ScheduledWeekDue(SaturdayTen.AddDays(2), null) is null).IsTrue();
        await Assert.That(planning.ScheduledWeekDue(SaturdayTen, NextSunday) is null).IsTrue();
    }
}
