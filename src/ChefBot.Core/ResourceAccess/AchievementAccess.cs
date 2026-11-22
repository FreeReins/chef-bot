using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IAchievementAccess"/>
internal sealed class AchievementAccess(ChefBotDbContext db) : IAchievementAccess
{
    public async Task<List<AchievementUnlock>> ListAsync(CancellationToken cancellationToken) =>
        (await db.AchievementUnlocks.AsNoTracking().OrderBy(unlock => unlock.EarnedAt).ThenBy(unlock => unlock.Id).ToListAsync(cancellationToken))
        .Select(unlock => unlock.ToContract())
        .ToList();

    public Task AddAsync(AchievementUnlock unlock, CancellationToken cancellationToken)
    {
        db.AchievementUnlocks.Add(unlock.ToEntity());
        return Task.CompletedTask;
    }
}
