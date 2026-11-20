namespace ChefBot.Core.Contracts.Messages;

/// <summary>A freshly created link. The token is only available now: it is stored as a hash.</summary>
public sealed record IssuedLink(Guid Id, LinkPurpose Purpose, string Token, DateTimeOffset ExpiresAt);
