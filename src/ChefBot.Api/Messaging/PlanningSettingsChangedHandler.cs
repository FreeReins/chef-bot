using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;

namespace ChefBot.Api.Messaging;

/// <summary>Brings the open plans' shopping lists in line with changed advanced planning settings, such as when food counts as expiring.</summary>
public static class PlanningSettingsChangedHandler
{
    public static Task Handle(PlanningSettingsChanged message, IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshOpenPlansAsync(cancellationToken);
}
