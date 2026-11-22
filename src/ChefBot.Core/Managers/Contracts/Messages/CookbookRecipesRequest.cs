namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Recipes to put in a cookbook. Ones already in it stay as they are.</summary>
/// <param name="RecipeIds">The recipes, each one that can be planned.</param>
public sealed record CookbookRecipesRequest(IReadOnlyList<Guid> RecipeIds);
