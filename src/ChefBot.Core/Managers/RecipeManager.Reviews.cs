using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>Reviews of recipes, and the ratings they add up to.</content>
internal sealed partial class RecipeManager
{
    public async Task<RecipeReviewsResponse> ListReviewsAsync(Guid id, CancellationToken cancellationToken) =>
        await ReviewsAsync((await ActiveAsync(id, cancellationToken)).Id, cancellationToken);

    public async Task<RecipeReviewsResponse> AddReviewAsync(
        Guid id, Guid userId, RecipeReviewRequest request, CancellationToken cancellationToken)
    {
        var (stars, notes) = ValidationEngine.Review(request);
        var recipe = await ActiveAsync(id, cancellationToken);
        var user = await validation.UserAsync(userId, cancellationToken);
        await reviews.AddAsync(
            new RecipeReview
            {
                RecipeId = recipe.Id,
                UserId = user.Id,
                ReviewerName = user.DisplayName,
                Stars = stars,
                Notes = notes,
                ReviewedOn = clock.Today(),
                CreatedAt = clock.GetUtcNow(),
            },
            cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ReviewsAsync(recipe.Id, cancellationToken);
    }

    public async Task<RecipeReviewsResponse> UpdateReviewAsync(
        Guid id, Guid reviewId, Guid userId, RecipeReviewRequest request, CancellationToken cancellationToken)
    {
        var (stars, notes) = ValidationEngine.Review(request);
        var recipe = await ActiveAsync(id, cancellationToken);
        var review = ValidationEngine.ReviewOf(await reviews.FindAsync(reviewId, cancellationToken), recipe);
        ValidationEngine.MayChange(review, userId);

        review.Stars = stars;
        review.Notes = notes;
        review.UpdatedAt = clock.GetUtcNow();
        await reviews.UpdateAsync(review, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ReviewsAsync(recipe.Id, cancellationToken);
    }

    public async Task<RecipeReviewsResponse> RemoveReviewAsync(
        Guid id, Guid reviewId, Guid userId, bool mayRemoveAnyone, CancellationToken cancellationToken)
    {
        var recipe = await ActiveAsync(id, cancellationToken);
        var review = ValidationEngine.ReviewOf(await reviews.FindAsync(reviewId, cancellationToken), recipe);
        ValidationEngine.MayChange(review, userId, mayRemoveAnyone);

        await reviews.RemoveAsync(review.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ReviewsAsync(recipe.Id, cancellationToken);
    }

    /// <summary>A recipe's reviews, newest first, with their average.</summary>
    private async Task<RecipeReviewsResponse> ReviewsAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        var all = await reviews.ListForRecipeAsync(recipeId, cancellationToken);
        var rating = all.Count == 0 ? null : new ReviewTally(all.Count, all.Sum(review => review.Stars)).ToRating();
        return new RecipeReviewsResponse(rating, all.Select(review => review.ToResponse()).ToList());
    }

    /// <summary>Loads the ratings of <paramref name="shown"/> at once, and returns each one's, or null when it has no reviews.</summary>
    private async Task<Func<Recipe, RecipeRating?>> RatedAsync(IReadOnlyCollection<Recipe> shown, CancellationToken cancellationToken)
    {
        var tallies = await reviews.TallyAsync(shown.Select(recipe => recipe.Id).ToList(), cancellationToken);
        return recipe => tallies.TryGetValue(recipe.Id, out var tally) ? tally.ToRating() : null;
    }
}
