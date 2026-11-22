using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a plan's meals or shopping list changed, by a person or by keeping it in step with the pantry.</summary>
/// <param name="PlanId">The plan.</param>
[MessageIdentity("chefbot.plan-changed.v1")]
public sealed record PlanChanged(Guid PlanId);
