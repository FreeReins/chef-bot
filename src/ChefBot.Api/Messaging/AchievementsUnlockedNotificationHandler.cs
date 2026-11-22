using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Announces new achievements through the notification webhook, when the household wants that.</summary>
[StickyHandler(MessageQueues.Notifications)]
public static class AchievementsUnlockedNotificationHandler
{
    public static async Task Handle(
        AchievementsUnlocked message, IMaintenanceManager maintenance, ILogger<AchievementsUnlocked> logger, CancellationToken cancellationToken)
    {
        var run = await maintenance.SendAchievementsAsync(message, cancellationToken);
        if (run.Problems.Count > 0)
        {
            logger.LogWarning("The achievements notification wasn't delivered: {Problems}", string.Join("; ", run.Problems));
        }
    }
}
