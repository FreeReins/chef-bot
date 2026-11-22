using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a plan was removed.</summary>
/// <param name="PlanId">The plan that was removed.</param>
[MessageIdentity("chefbot.plan-cleared.v1")]
public sealed record PlanCleared(Guid PlanId);
