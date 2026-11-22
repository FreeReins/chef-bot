namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe's reviews and the rating they add up to.</summary>
/// <param name="Rating">The average of the reviews, or null when there are none.</param>
/// <param name="Reviews">Every review, newest first.</param>
public sealed record RecipeReviewsResponse(RecipeRating? Rating, IReadOnlyList<RecipeReviewResponse> Reviews);
