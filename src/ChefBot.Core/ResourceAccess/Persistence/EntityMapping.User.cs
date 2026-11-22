using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies UserEntity to and from UserAccount.</content>
internal static partial class EntityMapping
{
    public static UserAccount ToContract(this UserEntity entity) => new()
    {
        Id = entity.Id,
        Username = entity.Username,
        DisplayName = entity.DisplayName,
        PasswordHash = entity.PasswordHash,
        IsAdmin = entity.IsAdmin,
        CreatedAt = entity.CreatedAt,
        FailedSignIns = entity.FailedSignIns,
        LockedUntil = entity.LockedUntil,
        SessionVersion = entity.SessionVersion,
        AchievementsSeenAt = entity.AchievementsSeenAt,
    };

    public static UserEntity ToEntity(this UserAccount user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        PasswordHash = user.PasswordHash,
        IsAdmin = user.IsAdmin,
        CreatedAt = user.CreatedAt,
        FailedSignIns = user.FailedSignIns,
        LockedUntil = user.LockedUntil,
        SessionVersion = user.SessionVersion,
        AchievementsSeenAt = user.AchievementsSeenAt,
    };
}
