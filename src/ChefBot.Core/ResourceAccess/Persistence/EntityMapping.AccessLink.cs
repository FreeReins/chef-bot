using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies AccessLinkEntity to and from AccessLink.</content>
internal static partial class EntityMapping
{
    public static AccessLink ToContract(this AccessLinkEntity entity) => new()
    {
        Id = entity.Id,
        Purpose = entity.Purpose,
        TokenHash = entity.TokenHash,
        UserId = entity.UserId,
        CreatedByUserId = entity.CreatedByUserId,
        CreatedAt = entity.CreatedAt,
        ExpiresAt = entity.ExpiresAt,
    };

    public static AccessLinkEntity ToEntity(this AccessLink link) => new()
    {
        Id = link.Id,
        Purpose = link.Purpose,
        TokenHash = link.TokenHash,
        UserId = link.UserId,
        CreatedByUserId = link.CreatedByUserId,
        CreatedAt = link.CreatedAt,
        ExpiresAt = link.ExpiresAt,
    };
}
