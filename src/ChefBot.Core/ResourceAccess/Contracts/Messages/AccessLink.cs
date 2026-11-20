using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A single-use, expiring link: an invitation to join, or a password reset for one account.</summary>
internal sealed class AccessLink
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public LinkPurpose Purpose { get; set; }

    /// <summary>A hash of the secret in the link, so a copy of the database can't be used to get in.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>The account a password reset is for. Null for invites.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The admin who created it. Null when it was created by the recovery setting at startup.</summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
