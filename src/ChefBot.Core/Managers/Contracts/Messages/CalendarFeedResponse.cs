namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The meal plan's calendar feed, which calendar apps can subscribe to.</summary>
/// <param name="Url">The feed's address, with its secret in it. Null when the feed is off.</param>
/// <param name="CreatedAt">When this address was made. Making a new one stops the old one working.</param>
public sealed record CalendarFeedResponse(string? Url, DateTimeOffset? CreatedAt);
