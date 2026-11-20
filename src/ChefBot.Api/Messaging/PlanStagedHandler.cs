using ChefBot.Core.Notifications;

namespace ChefBot.Api.Messaging;

/// <summary>Announces a newly staged plan through the notification webhook.</summary>
public sealed class PlanStagedHandler
{
    public Task Handle(PlanStaged message, ReminderService reminders, CancellationToken cancellationToken) =>
        reminders.SendPlanReadyAsync(message.PlanId, message.Trigger, cancellationToken);
}
