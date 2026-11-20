using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>The recipes read from an uploaded file, and how many entries in it couldn't be used.</summary>
internal sealed record RecipeFile(IReadOnlyList<RecipeText> Recipes, int Skipped);
