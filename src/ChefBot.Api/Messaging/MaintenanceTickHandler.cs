using ChefBot.Core.Backups;
using ChefBot.Core.Common;
using ChefBot.Core.Notifications;
using Wolverine;

namespace ChefBot.Api.Messaging;

/// <summary>Sends the daily reminders and runs the daily backup when they are due, then books the next check.</summary>
public sealed class MaintenanceTickHandler
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    public async Task Handle(
        MaintenanceTick message,
        ReminderService reminders,
        BackupService backups,
        IMessageBus bus,
        ILogger<MaintenanceTickHandler> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var run = await reminders.SendDailyIfDueAsync(cancellationToken);
            if (run is { Problems.Count: > 0 })
            {
                logger.LogWarning("Some reminders weren't delivered: {Problems}", string.Join("; ", run.Problems));
            }

            var backup = await backups.RunIfDueAsync(cancellationToken);
            if (backup is not null)
            {
                logger.LogInformation("Backup written: {Name}", backup.Name);
            }
        }
        catch (ChefBotException exception)
        {
            // Already recorded where an admin can see it; the next check tries again tomorrow.
            logger.LogWarning("Scheduled maintenance had a problem: {Reason}", exception.Message);
        }
        finally
        {
            await bus.ScheduleAsync(new MaintenanceTick(), Interval);
        }
    }
}
