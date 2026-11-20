namespace ChefBot.Core.Contracts.Messages;

/// <summary>A link that hasn't been used yet, as shown to admins. The token can't be shown again.</summary>
/// <param name="Id">The link's id.</param>
/// <param name="Purpose">Whether it is an invite or a password reset.</param>
/// <param name="Username">Whose password it resets. Null for invites.</param>
/// <param name="CreatedAt">When the link was created.</param>
/// <param name="ExpiresAt">When the link stops working.</param>
public sealed record PendingLink(Guid Id, LinkPurpose Purpose, string? Username, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
