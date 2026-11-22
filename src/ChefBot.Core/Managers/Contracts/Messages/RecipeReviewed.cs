using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: someone wrote or changed a review of a recipe.</summary>
/// <param name="RecipeId">The recipe.</param>
/// <param name="UserId">Who wrote it.</param>
/// <param name="ReviewId">The review.</param>
[MessageIdentity("chefbot.recipe-reviewed.v1")]
public sealed record RecipeReviewed(Guid RecipeId, Guid UserId, Guid ReviewId);
