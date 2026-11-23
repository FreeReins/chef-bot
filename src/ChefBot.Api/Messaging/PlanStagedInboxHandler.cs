using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>Tells everyone in their inbox when the weekly schedule stages a plan. Handling it twice adds nothing.</summary>
[StickyHandler(MessageQueues.Inbox)]
public static class PlanStagedInboxHandler
{
    public static Task Handle(PlanStaged message, Envelope envelope, IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.AnnouncePlanStagedAsync(message, envelope.Id, cancellationToken);
}
