using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Brings the open plans' shopping lists in line with recipes whose ingredients were filed under new names.</summary>
[StickyHandler(MessageQueues.Plans)]
public static class IngredientNamesChangedHandler
{
    public static Task Handle(IngredientNamesChanged message, IPlanManager plans, CancellationToken cancellationToken) =>
        plans.RefreshOpenPlansAsync(cancellationToken);
}
