using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a plan was confirmed, so its shopping list is the one to shop from.</summary>
/// <param name="PlanId">The plan.</param>
[MessageIdentity("chefbot.plan-confirmed.v1")]
public sealed record PlanConfirmed(Guid PlanId);
