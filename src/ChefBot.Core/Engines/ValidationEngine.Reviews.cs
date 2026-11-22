using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <content>Checks on reviews of recipes.</content>
internal sealed partial class ValidationEngine
{
    public const int MaxReviewNotesLength = 2000;

    /// <summary>The stars and notes of a review, with the notes trimmed and blank notes made null.</summary>
    public static (int Stars, string? Notes) Review(RecipeReviewRequest request)
    {
        if (request.Stars is < 1 or > 5)
        {
            throw new RuleViolationException("Choose from 1 to 5 stars.");
        }

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (notes?.Length > MaxReviewNotesLength)
        {
            throw new RuleViolationException(
                $"Review notes can be up to {MaxReviewNotesLength} characters. Shorten them and save again.");
        }

        return (request.Stars, notes);
    }

    /// <summary>The review, when it is one of that recipe's.</summary>
    public static RecipeReview ReviewOf(RecipeReview? review, Recipe recipe) =>
        review is not null && review.RecipeId == recipe.Id
            ? review
            : throw new NotFoundException($"That review of {recipe.Name} no longer exists.");

    /// <summary>Checks that a person may change a review: it is their own, or they may change anyone's.</summary>
    public static void MayChange(RecipeReview review, Guid userId, bool mayChangeAnyone = false)
    {
        if (review.UserId != userId && !mayChangeAnyone)
        {
            throw new NotAllowedException("You can only change your own reviews.");
        }
    }
}
