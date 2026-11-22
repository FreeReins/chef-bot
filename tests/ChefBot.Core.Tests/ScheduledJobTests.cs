using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

public class ScheduledJobTests
{
    private static readonly DateOnly Tuesday = TestData.Sunday.AddDays(2);

    private static DateTimeOffset At(DateOnly day, int hour, int minute = 0) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);

    [Test]
    public async Task The_next_check_is_now_when_due_otherwise_the_next_time_the_clock_comes_round()
    {
        var due = DailyTime.NextCheck("07:30", lastRunOn: null, At(Tuesday, 9).AddTicks(1234), TimeZoneInfo.Utc);
        var later = DailyTime.NextCheck("07:30", lastRunOn: null, At(Tuesday, 6), TimeZoneInfo.Utc);
        var done = DailyTime.NextCheck("07:30", lastRunOn: Tuesday, At(Tuesday, 9), TimeZoneInfo.Utc);

        await Assert.That(due).IsEqualTo(At(Tuesday, 9));
        await Assert.That(later).IsEqualTo(At(Tuesday, 7, 30));
        await Assert.That(done).IsEqualTo(At(Tuesday.AddDays(1), 7, 30));
    }

    [Test]
    public async Task Each_reminder_check_schedules_the_next_one_and_only_that_one_is_acted_on()
    {
        var core = new TestCore();
        core.Clock.Now = At(Tuesday, 9);
        core.Settings.Notifications.DailyTime = "07:30";

        await core.MaintenanceManager().SendDueRemindersAsync(new SendDueReminders(), default);
        var (message, at) = core.Messages.Scheduled.Single();
        var stale = await core.MaintenanceManager().SendDueRemindersAsync(new SendDueReminders(At(Tuesday, 8)), default);

        await Assert.That(core.Settings.Notifications.LastDailyRunOn).IsEqualTo(Tuesday);
        await Assert.That(at).IsEqualTo(At(Tuesday.AddDays(1), 7, 30));
        await Assert.That(((SendDueReminders)message).DueAt).IsEqualTo(at);
        await Assert.That(core.Settings.Notifications.RemindersDueAt).IsEqualTo(at);
        await Assert.That(stale).IsNull();
        await Assert.That(core.Messages.Scheduled.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Changing_the_reminder_time_schedules_a_new_check_that_replaces_the_old()
    {
        var core = new TestCore();
        core.Clock.Now = At(Tuesday, 6);
        await core.MaintenanceManager().SendDueRemindersAsync(new SendDueReminders(), default);
        var first = (SendDueReminders)core.Messages.Scheduled.Single().Message;

        await core.MaintenanceManager().UpdateNotificationSettingsAsync(
            new NotificationSettingsRequest(null, true, true, true, true, true, "06:30"), default);
        var replaced = await core.MaintenanceManager().SendDueRemindersAsync(first, default);

        await Assert.That(core.Settings.Notifications.RemindersDueAt).IsEqualTo(At(Tuesday, 6, 30));
        await Assert.That(replaced).IsNull();
    }

    [Test]
    public async Task Backups_switched_off_leave_nothing_scheduled()
    {
        var core = new TestCore();
        core.Clock.Now = At(Tuesday, 1);

        await core.MaintenanceManager().UpdateBackupSettingsAsync(new BackupSettingsRequest(false, "03:00", 14, 0), default);
        await core.MaintenanceManager().UpdateBackupSettingsAsync(new BackupSettingsRequest(true, "03:00", 14, 0), default);

        var scheduled = core.Messages.Scheduled.Select(entry => entry.Message).OfType<RunDueBackup>().ToList();
        await Assert.That(scheduled.Single().DueAt).IsEqualTo(At(Tuesday, 3));
    }

    [Test]
    public async Task A_weekly_check_that_was_replaced_does_nothing()
    {
        var core = new TestCore();
        core.Recipes.Rows.AddRange(TestData.Library);

        await core.PlanManager().CheckWeeklyScheduleAsync(new CheckWeeklySchedule(DateTimeOffset.UnixEpoch), default);

        await Assert.That(core.Plans.Rows.Count).IsEqualTo(0);
        await Assert.That(core.Messages.Scheduled.Count).IsEqualTo(0);
    }
}
