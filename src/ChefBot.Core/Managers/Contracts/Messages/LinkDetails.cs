namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What a link is for, shown to the person who opened it.</summary>
public sealed record LinkDetails(LinkPurpose Purpose, string? Username, DateTimeOffset ExpiresAt);
