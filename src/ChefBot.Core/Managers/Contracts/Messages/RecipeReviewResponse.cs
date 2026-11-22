namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One person's review of a recipe.</summary>
/// <param name="Id">The review's id.</param>
/// <param name="UserId">The reviewer's account id, to tell whether a review is the signed-in person's own.</param>
/// <param name="ReviewedBy">The reviewer's name when they wrote it.</param>
/// <param name="Stars">From 1 to 5.</param>
/// <param name="Notes">What they had to say, or null when they only rated it.</param>
/// <param name="At">When it was written, in UTC.</param>
/// <param name="Edited">True when it was changed after it was written.</param>
public sealed record RecipeReviewResponse(
    Guid Id, Guid UserId, string ReviewedBy, int Stars, string? Notes, DateTimeOffset At, bool Edited);
