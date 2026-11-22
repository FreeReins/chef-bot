using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a plan was staged and is ready for review.</summary>
/// <param name="PlanId">The staged plan.</param>
/// <param name="Trigger">What staged it: "schedule", "manual" or "regenerate".</param>
[MessageIdentity("chefbot.plan-staged.v1")]
public sealed record PlanStaged(Guid PlanId, string Trigger);
