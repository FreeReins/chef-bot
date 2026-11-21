using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Moving meals between days, as dragging them on the week board does.</summary>
public class MoveMealTests
{
    private static readonly DateTimeOffset SaturdayTen = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly NextSunday = new(2026, 11, 15);

    private static async Task<(TestCore Core, PlanResponse Plan)> StagedAsync()
    {
        var core = new TestCore();
        core.Clock.Now = SaturdayTen;
        core.Recipes.Rows.AddRange(TestData.Library);
        return (core, await core.PlanManager().StageAsync(new StagePlanRequest(), default));
    }

    private static MealResponse DinnerOn(PlanResponse plan, DayOfWeek day) =>
        plan.Meals.Single(meal => meal.Slot == MealSlot.Dinner && meal.Date!.Value.DayOfWeek == day);

    private static DateOnly Day(DayOfWeek day) => NextSunday.AddDays((int)day);

    [Test]
    public async Task Moving_a_dinner_to_an_empty_day_moves_its_leftovers_with_it()
    {
        var (core, plan) = await StagedAsync();
        var sunday = DinnerOn(plan, DayOfWeek.Sunday);

        var moved = await core.PlanManager().MoveMealAsync(plan.Id, sunday.Id, new MoveMealRequest(Day(DayOfWeek.Monday)), default);

        var monday = DinnerOn(moved, DayOfWeek.Monday);
        await Assert.That(monday.Id).IsEqualTo(sunday.Id);
        await Assert.That(moved.Meals.Any(meal => meal is { Slot: MealSlot.Dinner, Day: "Sunday" })).IsFalse();
        // Leftovers only come from earlier dinners, so Monday's lunch can no longer be the moved dinner.
        await Assert.That(moved.Meals.Any(meal => meal is { Slot: MealSlot.Lunch, Day: "Monday" } && meal.Title.EndsWith(sunday.Title))).IsFalse();
        await Assert.That(moved.Notes.Single()).IsEqualTo($"Moved {sunday.Title} to Monday.");
        await Assert.That(core.Plans.Rows.Single().Meals.Single(meal => meal.Id == sunday.Id).Date).IsEqualTo(Day(DayOfWeek.Monday));
    }

    [Test]
    public async Task Moving_a_dinner_onto_another_swaps_them_and_resizes_both()
    {
        var (core, plan) = await StagedAsync();
        var tuesday = DinnerOn(plan, DayOfWeek.Tuesday);
        var thursday = DinnerOn(plan, DayOfWeek.Thursday);

        var swapped = await core.PlanManager().MoveMealAsync(plan.Id, tuesday.Id, new MoveMealRequest(Day(DayOfWeek.Thursday)), default);

        await Assert.That(DinnerOn(swapped, DayOfWeek.Thursday).Id).IsEqualTo(tuesday.Id);
        await Assert.That(DinnerOn(swapped, DayOfWeek.Tuesday).Id).IsEqualTo(thursday.Id);
        await Assert.That(swapped.Notes.Single()).IsEqualTo($"Swapped {tuesday.Title} to Thursday and {thursday.Title} to Tuesday.");
        await Assert.That(swapped.Shopping.Count > 0).IsTrue();
    }

    [Test]
    public async Task Leftovers_snacks_cooked_meals_and_other_weeks_cant_move()
    {
        var (core, plan) = await StagedAsync();
        var plans = core.PlanManager();
        var lunch = plan.Meals.First(meal => meal.IsLeftover);
        var snack = plan.Meals.First(meal => meal.Slot == MealSlot.Snack);
        var sunday = DinnerOn(plan, DayOfWeek.Sunday);
        var monday = new MoveMealRequest(Day(DayOfWeek.Monday));

        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.MoveMealAsync(plan.Id, lunch.Id, monday, default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.MoveMealAsync(plan.Id, snack.Id, monday, default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() =>
            plans.MoveMealAsync(plan.Id, sunday.Id, new MoveMealRequest(NextSunday.AddDays(7)), default))).IsNotNull();

        await plans.SetMealStatusAsync(plan.Id, sunday.Id, new MealStatusRequest(MealStatus.Cooked), default);
        await Assert.That(await TestData.Caught<ConflictException>(() => plans.MoveMealAsync(plan.Id, sunday.Id, monday, default))).IsNotNull();
        var tuesday = DinnerOn(plan, DayOfWeek.Tuesday);
        await Assert.That(await TestData.Caught<ConflictException>(() =>
            plans.MoveMealAsync(plan.Id, tuesday.Id, new MoveMealRequest(NextSunday), default))).IsNotNull();
    }
}
