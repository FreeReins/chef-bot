using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IReviewAccess"/>
internal sealed class ReviewAccess(ChefBotDbContext db) : IReviewAccess
{
    public async Task<List<RecipeReview>> ListForRecipeAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        // Loading them tracks them, so one added or removed earlier in this request is seen.
        await db.RecipeReviews.Where(review => review.RecipeId == recipeId).LoadAsync(cancellationToken);
        return db.RecipeReviews.Local
            .Where(review => review.RecipeId == recipeId)
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.Id)
            .Select(review => review.ToContract())
            .ToList();
    }

    public async Task<List<RecipeReview>> ListAllAsync(CancellationToken cancellationToken) =>
        (await db.RecipeReviews.AsNoTracking().OrderBy(review => review.CreatedAt).ThenBy(review => review.Id).ToListAsync(cancellationToken))
            .Select(review => review.ToContract())
            .ToList();

    public async Task<Dictionary<Guid, ReviewTally>> TallyAsync(
        IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken)
    {
        if (recipeIds.Count == 0)
        {
            return [];
        }

        return await db.RecipeReviews.AsNoTracking()
            .Where(review => recipeIds.Contains(review.RecipeId))
            .GroupBy(review => review.RecipeId)
            .Select(group => new { RecipeId = group.Key, Count = group.Count(), Total = group.Sum(review => review.Stars) })
            .ToDictionaryAsync(row => row.RecipeId, row => new ReviewTally(row.Count, row.Total), cancellationToken);
    }

    public async Task<RecipeReview?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.RecipeReviews.FindAsync([id], cancellationToken))?.ToContract();

    public Task AddAsync(RecipeReview review, CancellationToken cancellationToken)
    {
        db.RecipeReviews.Add(review.ToEntity());
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(RecipeReview review, CancellationToken cancellationToken)
    {
        if (await db.RecipeReviews.FindAsync([review.Id], cancellationToken) is { } stored)
        {
            stored.Stars = review.Stars;
            stored.Notes = review.Notes;
            stored.UpdatedAt = review.UpdatedAt;
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.RecipeReviews.FindAsync([id], cancellationToken) is { } stored)
        {
            db.RecipeReviews.Remove(stored);
        }
    }
}
