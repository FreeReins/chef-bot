using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>The achievement tiers and chef ranks the household has reached, kept so each is announced once.</summary>
internal interface IAchievementAccess
{
    /// <summary>Everything reached so far, oldest first. A household has a few dozen at most.</summary>
    Task<List<AchievementUnlock>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(AchievementUnlock unlock, CancellationToken cancellationToken);
}
