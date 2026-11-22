using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: ChefBot was set up, with its first account.</summary>
/// <param name="UserId">Who set it up.</param>
/// <param name="DisplayName">Their name.</param>
/// <param name="OccurredOn">The day it was set up.</param>
/// <param name="OccurredAt">When, in UTC.</param>
[MessageIdentity("chefbot.kitchen-opened.v1")]
public sealed record KitchenOpened(Guid UserId, string DisplayName, DateOnly OccurredOn, DateTimeOffset OccurredAt) : IKitchenMoment;
