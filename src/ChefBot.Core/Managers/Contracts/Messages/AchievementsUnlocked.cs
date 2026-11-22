using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the household reached new achievement tiers or a new chef rank, saved and ready to announce.</summary>
/// <param name="Unlocks">What was reached, oldest first.</param>
[MessageIdentity("chefbot.achievements-unlocked.v1")]
public sealed record AchievementsUnlocked(IReadOnlyList<AchievementUnlockResponse> Unlocks);
