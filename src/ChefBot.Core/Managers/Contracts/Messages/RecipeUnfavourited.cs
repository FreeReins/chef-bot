using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: someone took a recipe out of their favourites.</summary>
/// <param name="RecipeId">The recipe.</param>
/// <param name="UserId">Who took it out.</param>
[MessageIdentity("chefbot.recipe-unfavourited.v1")]
public sealed record RecipeUnfavourited(Guid RecipeId, Guid UserId);
