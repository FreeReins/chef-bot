using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Announces a newly staged plan through the notification webhook.</summary>
[StickyHandler(MessageQueues.Notifications)]
public static class PlanStagedNotificationHandler
{
    public static async Task Handle(PlanStaged message, IMaintenanceManager maintenance, ILogger<PlanStaged> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendPlanReadyAsync(message, cancellationToken);
        if (run.Problems.Count > 0)
        {
            logger.LogWarning("The plan-ready notification wasn't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}
