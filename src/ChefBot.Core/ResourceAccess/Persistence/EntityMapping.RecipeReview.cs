using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies RecipeReviewEntity to and from RecipeReview.</content>
internal static partial class EntityMapping
{
    public static RecipeReview ToContract(this RecipeReviewEntity entity) => new()
    {
        Id = entity.Id,
        RecipeId = entity.RecipeId,
        UserId = entity.UserId,
        ReviewerName = entity.ReviewerName,
        Stars = entity.Stars,
        Notes = entity.Notes,
        ReviewedOn = entity.ReviewedOn,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };

    public static RecipeReviewEntity ToEntity(this RecipeReview review) => new()
    {
        Id = review.Id,
        RecipeId = review.RecipeId,
        UserId = review.UserId,
        ReviewerName = review.ReviewerName,
        Stars = review.Stars,
        Notes = review.Notes,
        ReviewedOn = review.ReviewedOn,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt,
    };
}
