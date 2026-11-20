namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>Everything read from a source, and how many recipes couldn't be read.</summary>
internal sealed record RecipeSourceResult(IReadOnlyList<SourceRecipe> Recipes, int Skipped);
