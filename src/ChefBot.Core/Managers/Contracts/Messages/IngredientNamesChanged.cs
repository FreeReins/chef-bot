using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: saved recipes' ingredients were filed under new names, so the open plans' shopping lists follow.</summary>
/// <param name="Recipes">How many recipes changed.</param>
[MessageIdentity("chefbot.ingredient-names-changed.v1")]
public sealed record IngredientNamesChanged(int Recipes);
