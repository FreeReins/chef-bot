using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Delivers one of the day's reminders to the webhook. A webhook that is down is tried again later by the message
/// bus; one that answers with a lasting problem, such as a wrong address, is logged.
/// </summary>
[StickyHandler(MessageQueues.Notifications)]
public static class SendReminderHandler
{
    public static async Task Handle(SendReminder command, IMaintenanceManager maintenance, ILogger<SendReminder> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendReminderAsync(command, cancellationToken);
        if (run.Problems.Count > 0)
        {
            logger.LogWarning("A reminder wasn't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}
