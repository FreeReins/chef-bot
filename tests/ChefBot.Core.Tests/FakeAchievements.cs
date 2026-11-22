using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeAchievements : IAchievementAccess
{
    public List<AchievementUnlock> Rows { get; } = [];

    public Task<List<AchievementUnlock>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.OrderBy(unlock => unlock.EarnedAt).ThenBy(unlock => unlock.Id).ToList());

    public Task AddAsync(AchievementUnlock unlock, CancellationToken cancellationToken)
    {
        Rows.Add(unlock);
        return Task.CompletedTask;
    }
}
