using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: suggestions for a recipe were made, applied or dismissed.</summary>
/// <param name="RecipeId">The recipe.</param>
[MessageIdentity("chefbot.recipe-suggestions-changed.v1")]
public sealed record RecipeSuggestionsChanged(Guid RecipeId);
