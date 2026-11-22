using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies ApiTokenEntity to and from ApiToken.</content>
internal static partial class EntityMapping
{
    public static ApiToken ToContract(this ApiTokenEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        Name = entity.Name,
        TokenHash = entity.TokenHash,
        CreatedAt = entity.CreatedAt,
    };

    public static ApiTokenEntity ToEntity(this ApiToken token) => new()
    {
        Id = token.Id,
        UserId = token.UserId,
        Name = token.Name,
        TokenHash = token.TokenHash,
        CreatedAt = token.CreatedAt,
    };
}
