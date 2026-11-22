using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Wolverine.Attributes;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Brings a plan's leftover lunches and shopping list in line with the pantry after a recipe is added from its page.
/// Safe to repeat: the list is worked out again from the saved plan and pantry each time.
/// </summary>
[StickyHandler(MessageQueues.Plans)]
public static class MealAddedHandler
{
    public static async Task Handle(MealAdded message, IPlanManager plans, ILogger<MealAdded> logger, CancellationToken cancellationToken)
    {
        // The plan can be cleared before this runs; there is nothing left to bring in line then.
        if (!await plans.CatchUpAsync(message.PlanId, cancellationToken))
        {
            logger.LogInformation("Plan {PlanId} was removed before its shopping list caught up with an added meal", message.PlanId);
        }
    }
}
