using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>
/// Kitchen achievements: the first steps, badges and chef rank, worked out from what is already kept. What was reached
/// is saved once (<see cref="CheckAchievementsAsync"/>, after each change that can earn something), so each new tier
/// is announced once, to everyone, on any device.
/// </content>
internal sealed partial class RecipeManager
{
    public async Task<AchievementsResponse> GetAchievementsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await validation.UserAsync(userId, cancellationToken);
        var unseen = (await unlocks.ListAsync(cancellationToken))
            .Where(unlock => !unlock.Quiet && (user.AchievementsSeenAt is null || unlock.EarnedAt > user.AchievementsSeenAt))
            .Select(unlock => unlock.ToResponse())
            .ToList();
        return await ScoreAsync(cancellationToken) with { Unseen = unseen };
    }

    public async Task MarkAchievementsSeenAsync(Guid userId, AchievementsSeenRequest request, CancellationToken cancellationToken)
    {
        var user = await validation.UserAsync(userId, cancellationToken);
        var shown = (await unlocks.ListAsync(cancellationToken)).FirstOrDefault(unlock => unlock.Id == request.UpTo);
        if (shown is null || shown.EarnedAt <= user.AchievementsSeenAt)
        {
            return;
        }

        user.AchievementsSeenAt = shown.EarnedAt;
        await accounts.SaveUserAsync(user, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AchievementUnlockResponse>> CheckAchievementsAsync(Guid? causedBy, CancellationToken cancellationToken)
    {
        var kept = await unlocks.ListAsync(cancellationToken);
        var quiet = kept.Count == 0;
        var fresh = AchievementEngine.Unlocks(await ScoreAsync(cancellationToken), kept, clock.GetUtcNow(), causedBy, quiet);
        if (fresh.Count == 0)
        {
            return [];
        }

        foreach (var unlock in fresh)
        {
            await unlocks.AddAsync(unlock, cancellationToken);
        }

        var announced = fresh.Where(unlock => !unlock.Quiet).Select(unlock => unlock.ToResponse()).ToList();
        if (announced.Count > 0)
        {
            await messages.PublishAsync(new AchievementsUnlocked(announced));
        }

        await messages.CommitAsync(cancellationToken);
        return announced;
    }

    private async Task<AchievementsResponse> ScoreAsync(CancellationToken cancellationToken) =>
        achievements.Score(
            await history.ListAsync(cancellationToken),
            await recipes.CountActiveAsync(cancellationToken),
            await favourites.CountAsync(cancellationToken),
            await reviews.CountAsync(cancellationToken),
            await plans.TallyAsync(cancellationToken));
}
