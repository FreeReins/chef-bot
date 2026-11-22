using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Sends the daily reminders when their time has come.</summary>
[StickyHandler(MessageQueues.Maintenance)]
public static class SendDueRemindersHandler
{
    public static async Task Handle(SendDueReminders command, IMaintenanceManager maintenance, ILogger<SendDueReminders> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendDueRemindersAsync(command, cancellationToken);
        if (run is { Problems.Count: > 0 })
        {
            logger.LogWarning("Some reminders weren't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}
