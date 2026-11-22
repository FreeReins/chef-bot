using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: someone accepted an invite and joined the household.</summary>
/// <param name="UserId">Who joined.</param>
/// <param name="DisplayName">Their name when they joined.</param>
/// <param name="OccurredOn">The day they joined.</param>
/// <param name="OccurredAt">When, in UTC.</param>
[MessageIdentity("chefbot.member-joined.v1")]
public sealed record MemberJoined(Guid UserId, string DisplayName, DateOnly OccurredOn, DateTimeOffset OccurredAt) : IKitchenMoment;
