using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: someone marked a recipe as a favourite.</summary>
/// <param name="RecipeId">The recipe.</param>
/// <param name="UserId">Who marked it.</param>
[MessageIdentity("chefbot.recipe-favourited.v1")]
public sealed record RecipeFavourited(Guid RecipeId, Guid UserId);
