using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;

namespace ChefBot.Api.Messaging;

/// <summary>Brings the open plans' shopping lists in line with a pantry that was edited by hand.</summary>
public static class PantryChangedHandler
{
    public static Task Handle(PantryChanged message, IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshOpenPlansAsync(cancellationToken);
}
