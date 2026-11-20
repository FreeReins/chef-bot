using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class AccessLinkEntity
{
    public Guid Id { get; set; }

    public LinkPurpose Purpose { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
