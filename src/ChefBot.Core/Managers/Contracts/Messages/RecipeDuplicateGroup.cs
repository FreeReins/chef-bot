namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Recipes with the same name once case, accents and punctuation are ignored.</summary>
/// <param name="Recipes">The recipes, by name.</param>
public sealed record RecipeDuplicateGroup(IReadOnlyList<RecipeRef> Recipes);
