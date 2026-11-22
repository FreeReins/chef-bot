using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>Reviews kept in a list.</summary>
internal sealed class FakeReviews : IReviewAccess
{
    public List<RecipeReview> Rows { get; } = [];

    public Task<List<RecipeReview>> ListForRecipeAsync(Guid recipeId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(row => row.RecipeId == recipeId)
            .OrderByDescending(row => row.CreatedAt).ThenByDescending(row => row.Id).Select(Copy).ToList());

    public Task<List<RecipeReview>> ListAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Select(Copy).ToList());

    public Task<Dictionary<Guid, ReviewTally>> TallyAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(row => recipeIds.Contains(row.RecipeId))
            .GroupBy(row => row.RecipeId)
            .ToDictionary(group => group.Key, group => new ReviewTally(group.Count(), group.Sum(row => row.Stars))));

    public Task<RecipeReview?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(row => row.Id == id) is { } row ? Copy(row) : null);

    public Task AddAsync(RecipeReview review, CancellationToken cancellationToken)
    {
        Rows.Add(Copy(review));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RecipeReview review, CancellationToken cancellationToken)
    {
        if (Rows.FirstOrDefault(row => row.Id == review.Id) is { } stored)
        {
            stored.Stars = review.Stars;
            stored.Notes = review.Notes;
            stored.UpdatedAt = review.UpdatedAt;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(row => row.Id == id);
        return Task.CompletedTask;
    }

    private static RecipeReview Copy(RecipeReview row) => new()
    {
        Id = row.Id, RecipeId = row.RecipeId, UserId = row.UserId, ReviewerName = row.ReviewerName, Stars = row.Stars,
        Notes = row.Notes, ReviewedOn = row.ReviewedOn, CreatedAt = row.CreatedAt, UpdatedAt = row.UpdatedAt,
    };
}
