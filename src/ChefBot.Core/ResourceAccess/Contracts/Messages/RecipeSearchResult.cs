namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A slice of the recipes matching a <see cref="RecipeQuery"/>, and how many match in all.</summary>
internal sealed record RecipeSearchResult(List<Recipe> Recipes, int Total);
