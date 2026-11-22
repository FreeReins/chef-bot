using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Brings the open plans' shopping lists in line with changed grocery stores.</summary>
[StickyHandler(MessageQueues.Plans)]
public static class GroceryStoresChangedHandler
{
    public static Task Handle(GroceryStoresChanged message, IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshOpenPlansAsync(cancellationToken);
}
