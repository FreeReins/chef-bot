using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies AchievementUnlockEntity to and from AchievementUnlock.</content>
internal static partial class EntityMapping
{
    /// <summary>The <see cref="AchievementUnlockEntity.Key"/> of a chef rank.</summary>
    public const string RankKey = "Rank";

    public static AchievementUnlock ToContract(this AchievementUnlockEntity entity) => new()
    {
        Id = entity.Id,
        Kind = entity.Key == RankKey ? null : Enum.Parse<AchievementKind>(entity.Key),
        Tier = entity.Tier,
        EarnedAt = entity.EarnedAt,
        Quiet = entity.Quiet,
        CausedBy = entity.CausedBy,
    };

    public static AchievementUnlockEntity ToEntity(this AchievementUnlock unlock) => new()
    {
        Id = unlock.Id,
        Key = unlock.Kind?.ToString() ?? RankKey,
        Tier = unlock.Tier,
        EarnedAt = unlock.EarnedAt,
        Quiet = unlock.Quiet,
        CausedBy = unlock.CausedBy,
    };
}
