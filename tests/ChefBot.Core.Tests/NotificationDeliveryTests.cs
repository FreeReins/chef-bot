using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class NotificationDeliveryTests
{
    /// <summary>The Tuesday of the test week: the slow cooker stew is tonight.</summary>
    private static readonly DateOnly Tuesday = TestData.Sunday.AddDays(2);

    private static TestCore Kitchen()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(Tuesday.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        core.Plans.Rows.Add(TestData.PlanWeek(TestData.Sunday).Plan);
        core.Settings.Notifications.WebhookUrl = "https://hooks.example/chefbot";
        return core;
    }

    [Test]
    public async Task The_days_reminders_are_queued_and_marked_done_in_one_commit()
    {
        var core = Kitchen();

        var run = await core.MaintenanceManager().SendTodayAsync(default);

        var queued = core.Messages.Published.OfType<SendReminder>().ToList();
        await Assert.That(queued.Select(reminder => reminder.Type)).Contains(NotificationTypes.SlowCooker);
        await Assert.That(queued.All(reminder => reminder.On == Tuesday)).IsTrue();
        await Assert.That(run.Sent).IsEquivalentTo(queued.Select(reminder => reminder.Type));
        await Assert.That(core.Settings.Notifications.LastDailyRunOn).IsEqualTo(Tuesday);
        await Assert.That(core.Messages.Commits).IsEqualTo(1);
        await Assert.That(core.Webhook.Sent.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Reminders_the_household_turned_off_are_not_queued()
    {
        var core = Kitchen();
        core.Settings.Notifications.SlowCooker = false;

        await core.MaintenanceManager().SendTodayAsync(default);

        await Assert.That(core.Messages.Published.OfType<SendReminder>().Any(reminder => reminder.Type == NotificationTypes.SlowCooker)).IsFalse();
    }

    [Test]
    public async Task A_queued_reminder_is_worded_when_it_goes_out()
    {
        var core = Kitchen();

        var run = await core.MaintenanceManager().SendReminderAsync(new SendReminder(NotificationTypes.SlowCooker, Tuesday), default);

        await Assert.That(run.Sent.Single()).IsEqualTo(NotificationTypes.SlowCooker);
        await Assert.That(core.Webhook.Sent.Single().Message.Contains(TestData.Stew.Name)).IsTrue();
    }

    [Test]
    public async Task A_reminder_that_no_longer_applies_is_dropped()
    {
        var core = Kitchen();

        var run = await core.MaintenanceManager().SendReminderAsync(new SendReminder(NotificationTypes.SlowCooker, Tuesday.AddDays(2)), default);

        await Assert.That(run.Sent.Count).IsEqualTo(0);
        await Assert.That(core.Webhook.Sent.Count).IsEqualTo(0);
    }

    [Test]
    public async Task A_webhook_that_is_down_is_left_for_the_message_bus_to_retry()
    {
        var core = Kitchen();
        core.Webhook.Answer = new Delivery(false, "The webhook answered HTTP 503.", Transient: true);

        await Assert.That(async () => await core.MaintenanceManager().SendReminderAsync(new SendReminder(NotificationTypes.SlowCooker, Tuesday), default))
            .Throws<TransientDeliveryException>();
    }

    [Test]
    public async Task A_lasting_webhook_problem_is_reported_not_retried()
    {
        var core = Kitchen();
        core.Webhook.Answer = new Delivery(false, "The webhook answered HTTP 404.");

        var run = await core.MaintenanceManager().SendReminderAsync(new SendReminder(NotificationTypes.SlowCooker, Tuesday), default);

        await Assert.That(run.Problems.Single().Contains("404")).IsTrue();
    }

    [Test]
    public async Task New_achievements_are_announced_unless_turned_off()
    {
        var core = Kitchen();
        var unlocked = new AchievementsUnlocked([new AchievementUnlockResponse(Guid.NewGuid(), AchievementKind.Dinners, 2, core.Clock.Now)]);

        await core.MaintenanceManager().SendAchievementsAsync(unlocked, default);
        core.Settings.Notifications.Achievements = false;
        await core.MaintenanceManager().SendAchievementsAsync(unlocked, default);

        await Assert.That(core.Webhook.Sent.Single().Title).IsEqualTo("New silver badge: Weeknight Hero");
    }
}
