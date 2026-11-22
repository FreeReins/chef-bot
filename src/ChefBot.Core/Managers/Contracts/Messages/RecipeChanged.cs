using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a recipe was edited, or its photos changed.</summary>
/// <param name="RecipeId">The recipe.</param>
[MessageIdentity("chefbot.recipe-changed.v1")]
public sealed record RecipeChanged(Guid RecipeId);
