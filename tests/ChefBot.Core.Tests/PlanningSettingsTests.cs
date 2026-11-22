using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>The settings saved in the app: people, the weekly schedule, leftovers, calendar times and the advanced ones.</summary>
public class PlanningSettingsTests
{
    /// <summary>A Saturday morning, after the default schedule time (Saturday 09:00).</summary>
    private static readonly DateTimeOffset SaturdayTen = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly NextSunday = new(2026, 11, 15);

    private static readonly Uri Site = new("https://chefbot.local:8443/");

    private static TestCore WithLibrary()
    {
        var core = new TestCore();
        core.Clock.Now = SaturdayTen;
        core.Recipes.Rows.AddRange(TestData.Library);
        return core;
    }

    private static PlannerSettingsRequest Planning(int people, int maxLeftoverDays = 3, bool scheduleEnabled = true, string time = "09:00") =>
        new(people, scheduleEnabled, DayOfWeek.Saturday, time, maxLeftoverDays);

    [Test]
    public async Task A_plan_for_one_person_makes_single_portions_and_dinners_cover_their_leftovers()
    {
        var core = WithLibrary();
        core.Settings.Planner.People = 1;

        var plan = await core.PlanManager().StageAsync(new StagePlanRequest(), default);
        var stored = core.Plans.Rows.Single();

        await Assert.That(stored.People).IsEqualTo(1);
        await Assert.That(plan.Meals.Where(meal => meal.Slot == MealSlot.Breakfast).All(meal => meal.Portions == 1)).IsTrue();
        foreach (var dinner in stored.Meals.Where(meal => meal.Slot == MealSlot.Dinner))
        {
            var lunches = stored.Meals.Count(meal => meal.LeftoverOfMealId == dinner.Id);
            await Assert.That(dinner.Portions).IsEqualTo(1 + lunches);
        }

        await Assert.That(stored.Meals.Any(meal => meal.Slot == MealSlot.Dinner && meal.Portions == 3)).IsTrue();
    }

    [Test]
    public async Task Changing_the_people_resizes_staged_weeks_but_not_confirmed_ones()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var confirmed = await plans.StageAsync(new StagePlanRequest(), default);
        await plans.ConfirmAsync(confirmed.Id, default);
        var staged = await plans.StageAsync(new StagePlanRequest(NextSunday.AddDays(7)), default);
        var shoppingBefore = core.Plans.Rows.Single(plan => plan.Id == staged.Id).ShoppingItems.Sum(item => item.Quantity ?? 0);

        var saved = await plans.SavePlannerSettingsAsync(Planning(people: 4), default);

        var resized = core.Plans.Rows.Single(plan => plan.Id == staged.Id);
        var kept = core.Plans.Rows.Single(plan => plan.Id == confirmed.Id);
        await Assert.That(saved.People).IsEqualTo(4);
        await Assert.That(saved.IsSaved).IsTrue();
        await Assert.That(resized.People).IsEqualTo(4);
        await Assert.That(resized.Meals.Where(meal => meal.Slot == MealSlot.Breakfast).All(meal => meal.Portions == 4)).IsTrue();
        await Assert.That(resized.ShoppingItems.Sum(item => item.Quantity ?? 0) > shoppingBefore).IsTrue();
        await Assert.That(kept.People).IsEqualTo(PlannerSettings.DefaultPeople);
        await Assert.That(kept.Meals.Where(meal => meal.Slot == MealSlot.Breakfast).All(meal => meal.Portions == 2)).IsTrue();
    }

    [Test]
    public async Task A_switched_off_schedule_stages_nothing_and_says_so()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();

        var saved = await plans.SavePlannerSettingsAsync(Planning(people: 2, scheduleEnabled: false), default);
        await plans.CheckWeeklyScheduleAsync(new CheckWeeklySchedule(), default);

        await Assert.That(saved.NextStaging).IsNull();
        await Assert.That(await plans.NextScheduledStagingAsync(default)).IsNull();
        await Assert.That(core.Plans.Rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task The_saved_schedule_time_decides_when_the_week_is_due()
    {
        var later = new PlannerSettings { ScheduleDay = DayOfWeek.Saturday, ScheduleTime = "11:00" };

        await Assert.That(PlanningEngine.ScheduledWeekDue(later, SaturdayTen, null) is null).IsTrue();
        await Assert.That(PlanningEngine.ScheduledWeekDue(later, SaturdayTen.AddHours(1), null)).IsEqualTo(NextSunday);
    }

    [Test]
    public async Task No_leftover_days_plans_no_leftover_lunches()
    {
        var core = WithLibrary();
        core.Settings.Planner.MaxLeftoverDays = 0;

        await core.PlanManager().StageAsync(new StagePlanRequest(), default);

        await Assert.That(core.Plans.Rows.Single().Meals.Any(meal => meal.IsLeftover)).IsFalse();
    }

    [Test]
    public async Task Planning_settings_out_of_range_are_refused()
    {
        var plans = new TestCore().PlanManager();

        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.SavePlannerSettingsAsync(Planning(people: 0), default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.SavePlannerSettingsAsync(Planning(people: 2, time: "9am"), default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.SavePlannerSettingsAsync(Planning(people: 2, maxLeftoverDays: 8), default))).IsNotNull();
    }

    [Test]
    public async Task The_calendar_feed_uses_the_saved_meal_times()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        await plans.StartCalendarFeedAsync(Site, default);
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        await plans.ConfirmAsync(plan.Id, default);

        await plans.SaveCalendarTimesAsync(new CalendarTimesRequest("07:00", "12:00", "19:15", 60, 0), default);
        var feed = await plans.GetCalendarAsync(core.Settings.CalendarFeed.Token, Site, default);

        await Assert.That(feed).Contains($"DTSTART:{NextSunday:yyyyMMdd}T191500");
        await Assert.That(feed).Contains($"DTEND:{NextSunday:yyyyMMdd}T201500");
        await Assert.That(feed).DoesNotContain("BEGIN:VALARM");
        await Assert.That(await TestData.Caught<RuleViolationException>(
            () => plans.SaveCalendarTimesAsync(new CalendarTimesRequest("07:00", "12:00", "19:15", 1, 0), default))).IsNotNull();
    }

    [Test]
    public async Task An_import_sorts_recipes_with_the_saved_course_words()
    {
        var core = new TestCore();
        core.Settings.Advanced.Words.SweetWords.Add("pudding");

        await core.RecipeManager().ImportFileAsync(
            new ImportFileRequest("pudding.json", """[{"name": "Rice Pudding", "keywords": "Pudding", "recipeIngredient": ["1 cup brown rice"]}]"""),
            default);

        await Assert.That(core.Recipes.Rows.Single().Course).IsEqualTo(RecipeCourse.Sweet);
    }

    [Test]
    public async Task Advanced_settings_left_out_go_back_to_their_defaults_and_words_are_tidied()
    {
        var defaults = new AdvancedSettings();

        var reset = ValidationEngine.Advanced(new AdvancedSettingsRequest(null, null, null, null));
        var tidied = ValidationEngine.Advanced(new AdvancedSettingsRequest(
            14, 5, null, new CourseWords(null, null, [" Pudding ", "pudding", ""], null, null, null, ["Dinner"])));

        await Assert.That(reset.AvoidRepeatDays).IsEqualTo(defaults.AvoidRepeatDays);
        await Assert.That(string.Join(", ", reset.Words.SweetWords)).IsEqualTo(string.Join(", ", defaults.Words.SweetWords));
        await Assert.That(tidied.AvoidRepeatDays).IsEqualTo(14);
        await Assert.That(tidied.PerishableWithinDays).IsEqualTo(defaults.PerishableWithinDays);
        await Assert.That(string.Join(", ", tidied.Words.SweetWords)).IsEqualTo("pudding");
        await Assert.That(string.Join(", ", tidied.Words.MainWords)).IsEqualTo("dinner");
        await Assert.That(string.Join(", ", tidied.Words.BreakfastWords)).IsEqualTo(string.Join(", ", defaults.Words.BreakfastWords));
        await Assert.That(await TestData.Caught<RuleViolationException>(() =>
        {
            ValidationEngine.Advanced(new AdvancedSettingsRequest(null, null, 0, null));
            return Task.CompletedTask;
        })).IsNotNull();
    }

    [Test]
    public async Task Repeats_are_avoided_for_the_saved_number_of_days()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var first = await plans.StageAsync(new StagePlanRequest(), default);
        var firstDinners = first.Meals.Where(meal => meal.Slot == MealSlot.Dinner).Select(meal => meal.RecipeId).ToHashSet();

        core.Settings.Advanced.AvoidRepeatDays = 0;
        var next = await plans.StageAsync(new StagePlanRequest(NextSunday.AddDays(7)), default);

        // With no days to avoid, last week's recipes are allowed again; the library is small enough that some come back.
        await Assert.That(next.Meals.Any(meal => meal.Slot == MealSlot.Dinner && firstDinners.Contains(meal.RecipeId))).IsTrue();
    }
}
