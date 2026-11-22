namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>An achievement tier, or a chef rank, the household reached, as kept when it was first reached.</summary>
/// <param name="Id">Identifies it, for saying it was seen (<c>POST /api/achievements/seen</c>).</param>
/// <param name="Kind">The achievement; null for a new chef rank.</param>
/// <param name="Tier">The tier reached (1 for a first step, 1 to 3 for bronze to gold), or the rank number for a chef rank.</param>
/// <param name="EarnedAt">When ChefBot noticed it, in UTC.</param>
public sealed record AchievementUnlockResponse(Guid Id, AchievementKind? Kind, int Tier, DateTimeOffset EarnedAt);
