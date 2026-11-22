using System.Text;
using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>The meal plan as a calendar feed, and the secret that guards it.</summary>
public class CalendarTests
{
    /// <summary>A Saturday morning, so staging plans the week that starts the next day.</summary>
    private static readonly DateTimeOffset Saturday = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly Uri Site = new("https://chefbot.local:8443/");

    private static readonly CalendarEngine Calendar = new();

    private static readonly CalendarSettings Times = new();

    private static readonly DateTimeOffset Now = new(2026, 11, 7, 9, 30, 0, TimeSpan.Zero);

    private static MealPlan PlanOf(params PlannedMeal[] meals)
    {
        var plan = new MealPlan { WeekStart = TestData.Sunday, Status = PlanStatus.Confirmed };
        plan.Meals.AddRange(meals);
        return plan;
    }

    /// <summary>The feed's lines with folded ones joined back up, as a calendar app reads them.</summary>
    private static List<string> Unfolded(string feed) =>
        feed.Replace("\r\n ", string.Empty, StringComparison.Ordinal).Split("\r\n", StringSplitOptions.RemoveEmptyEntries).ToList();

    [Test]
    public async Task A_dinner_is_an_event_at_dinner_time_linking_to_its_recipe()
    {
        var dinner = TestData.Meal(TestData.Tacos, MealSlot.Dinner, TestData.Sunday, portions: 4);

        var lines = Unfolded(Calendar.Write([PlanOf(dinner)], Site, Now, Times));

        await Assert.That(lines[0]).IsEqualTo("BEGIN:VCALENDAR");
        await Assert.That(lines[^1]).IsEqualTo("END:VCALENDAR");
        await Assert.That(lines).Contains($"UID:{dinner.Id}@chefbot");
        await Assert.That(lines).Contains("DTSTAMP:20261107T093000Z");
        await Assert.That(lines).Contains("DTSTART:20261108T183000");
        await Assert.That(lines).Contains("DTEND:20261108T191500");
        await Assert.That(lines).Contains($"SUMMARY:Dinner: {TestData.Tacos.Name}");
        await Assert.That(lines).Contains($"URL:https://chefbot.local:8443/recipes/{TestData.Tacos.Id}");
        await Assert.That(lines).DoesNotContain("BEGIN:VALARM");
    }

    [Test]
    public async Task Each_slot_has_its_own_time_and_the_times_can_be_changed()
    {
        var times = new CalendarSettings { BreakfastTime = "07:15", LunchTime = "12:00", MealMinutes = 30 };
        var plan = PlanOf(
            TestData.Meal(TestData.Tacos, MealSlot.Breakfast, TestData.Sunday, portions: 2),
            TestData.Meal(TestData.Tacos, MealSlot.Lunch, TestData.Sunday, portions: 2));

        var lines = Unfolded(Calendar.Write([plan], Site, Now, times));

        await Assert.That(lines).Contains("DTSTART:20261108T071500");
        await Assert.That(lines).Contains("DTEND:20261108T074500");
        await Assert.That(lines).Contains("DTSTART:20261108T120000");
    }

    [Test]
    public async Task A_slow_cooker_dinner_reminds_you_to_start_it_hours_before()
    {
        var stew = TestData.Meal(TestData.Stew, MealSlot.Dinner, TestData.Sunday, portions: 4);

        var lines = Unfolded(Calendar.Write([PlanOf(stew)], Site, Now, Times));

        await Assert.That(lines).Contains("BEGIN:VALARM");
        await Assert.That(lines).Contains("TRIGGER:-PT8H");
        await Assert.That(lines).Contains($"DESCRIPTION:Start the slow cooker for {TestData.Stew.Name}.");
    }

    [Test]
    public async Task Skipped_meals_and_meals_without_a_day_are_left_out()
    {
        var skipped = TestData.Meal(TestData.Tacos, MealSlot.Dinner, TestData.Sunday, portions: 4);
        skipped.Status = MealStatus.Skipped;
        var snack = TestData.Meal(TestData.Tacos, MealSlot.Snack, null, portions: 4);

        var feed = Calendar.Write([PlanOf(skipped, snack)], Site, Now, Times);

        await Assert.That(feed).DoesNotContain("BEGIN:VEVENT");
    }

    [Test]
    public async Task Text_is_escaped_and_long_lines_are_folded_without_splitting_a_character()
    {
        var text = new StringBuilder();
        CalendarEngine.Fold(text, $"SUMMARY:{CalendarEngine.Escape("Crème brûlée, again; with \\ and\nmore " + new string('é', 60))}");
        var lines = text.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        await Assert.That(lines.All(line => Encoding.UTF8.GetByteCount(line) <= 75)).IsTrue();
        await Assert.That(lines.Skip(1).All(line => line.StartsWith(' '))).IsTrue();
        await Assert.That(string.Concat(lines.Select((line, index) => index == 0 ? line : line[1..])))
            .IsEqualTo("SUMMARY:Crème brûlée\\, again\\; with \\\\ and\\nmore " + new string('é', 60));
    }

    [Test]
    public async Task The_feed_is_off_until_an_admin_turns_it_on()
    {
        var plans = new TestCore().PlanManager();

        var feed = await plans.GetCalendarFeedAsync(Site, default);

        await Assert.That(feed.Url).IsNull();
        await Assert.That(await TestData.Caught<NotFoundException>(() => plans.GetCalendarAsync(string.Empty, Site, default))).IsNotNull();
    }

    [Test]
    public async Task Only_the_current_address_opens_the_feed_and_turning_it_off_closes_it()
    {
        var core = new TestCore();
        var plans = core.PlanManager();

        var first = await plans.StartCalendarFeedAsync(Site, default);
        var second = await plans.StartCalendarFeedAsync(Site, default);
        var token = core.Settings.CalendarFeed.Token;

        await Assert.That(second.Url).IsEqualTo($"https://chefbot.local:8443/api/calendar/{token}/meals.ics");
        await Assert.That(first.Url).IsNotEqualTo(second.Url);
        await Assert.That(await plans.GetCalendarAsync(token, Site, default)).StartsWith("BEGIN:VCALENDAR");
        var oldToken = first.Url!.Split('/')[^2];
        await Assert.That(await TestData.Caught<NotFoundException>(() => plans.GetCalendarAsync(oldToken, Site, default))).IsNotNull();

        await plans.StopCalendarFeedAsync(default);
        await Assert.That(await TestData.Caught<NotFoundException>(() => plans.GetCalendarAsync(token, Site, default))).IsNotNull();
        await Assert.That((await plans.GetCalendarFeedAsync(Site, default)).Url).IsNull();
    }

    [Test]
    public async Task Only_confirmed_weeks_are_in_the_feed()
    {
        var core = new TestCore();
        core.Clock.Now = Saturday;
        core.Recipes.Rows.AddRange(TestData.Library);
        var plans = core.PlanManager();
        await plans.StartCalendarFeedAsync(Site, default);
        var token = core.Settings.CalendarFeed.Token;

        var draft = await plans.StageAsync(new StagePlanRequest(), default);
        var whileDraft = await plans.GetCalendarAsync(token, Site, default);
        await plans.ConfirmAsync(draft.Id, default);
        var confirmed = await plans.GetCalendarAsync(token, Site, default);

        await Assert.That(whileDraft).DoesNotContain("BEGIN:VEVENT");
        await Assert.That(Unfolded(confirmed).Count(line => line == "BEGIN:VEVENT"))
            .IsEqualTo(draft.Meals.Count(meal => meal.Date is not null));
    }
}
