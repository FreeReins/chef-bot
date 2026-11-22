using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: everything left on the shopping list was bought in one go.</summary>
/// <param name="PlanId">The plan shopped for, if any.</param>
/// <param name="Count">How many things were bought.</param>
/// <param name="OccurredOn">The day of the shop.</param>
/// <param name="OccurredAt">When, in UTC; null when the shop was recorded on a later day.</param>
[MessageIdentity("chefbot.shopping-done.v1")]
public sealed record ShoppingDone(Guid? PlanId, int Count, DateOnly OccurredOn, DateTimeOffset? OccurredAt) : IKitchenMoment;
