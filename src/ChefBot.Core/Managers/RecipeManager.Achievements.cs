using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Kitchen achievements: the first steps, badges and chef rank, worked out from what is already kept.</content>
internal sealed partial class RecipeManager
{
    public async Task<AchievementsResponse> GetAchievementsAsync(CancellationToken cancellationToken) =>
        achievements.Score(
            await history.ListAsync(cancellationToken),
            await recipes.CountActiveAsync(cancellationToken),
            (await favourites.ListAllAsync(cancellationToken)).Count,
            (await reviews.ListAllAsync(cancellationToken)).Count,
            await plans.TallyAsync(cancellationToken));
}
