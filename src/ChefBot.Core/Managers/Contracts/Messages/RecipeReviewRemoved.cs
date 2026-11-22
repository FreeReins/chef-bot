using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a review of a recipe was removed.</summary>
/// <param name="RecipeId">The recipe.</param>
/// <param name="ReviewId">The review.</param>
[MessageIdentity("chefbot.recipe-review-removed.v1")]
public sealed record RecipeReviewRemoved(Guid RecipeId, Guid ReviewId);
