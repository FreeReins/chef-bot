using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Achievements reached, as responses.</content>
internal static partial class Mapping
{
    public static AchievementUnlockResponse ToResponse(this AchievementUnlock unlock) =>
        new(unlock.Id, unlock.Kind, unlock.Tier, unlock.EarnedAt);
}
