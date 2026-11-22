using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Takes a removed cookbook out of the plan styles' cookbook rules, so no rule names a cookbook that has gone. Safe to
/// repeat: once the cookbook is out of every rule, running it again changes nothing.
/// </summary>
[StickyHandler(MessageQueues.Plans)]
public static class CookbookRemovedHandler
{
    public static async Task Handle(CookbookRemoved message, IPlanManager plans, ILogger<CookbookRemoved> logger, CancellationToken cancellationToken)
    {
        var dropped = await plans.ForgetCookbookAsync(message.CookbookId, cancellationToken);
        if (dropped > 0)
        {
            logger.LogInformation("Removed {Count} plan style rules that only named cookbook {CookbookId}, which was removed", dropped, message.CookbookId);
        }
    }
}
