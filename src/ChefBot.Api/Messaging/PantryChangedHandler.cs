using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Brings the open plans' shopping lists in line with a pantry that was edited by hand.</summary>
[StickyHandler(MessageQueues.Plans)]
public static class PantryChangedHandler
{
    public static Task Handle(PantryChanged message, IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshOpenPlansAsync(cancellationToken);
}
