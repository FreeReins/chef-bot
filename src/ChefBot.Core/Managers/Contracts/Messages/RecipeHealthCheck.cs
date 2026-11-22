namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One check run over the recipe library, and the recipes that fail it.</summary>
/// <param name="Issue">What the check looks for.</param>
/// <param name="Recipes">The recipes with that issue, by name. Empty when every recipe passes.</param>
public sealed record RecipeHealthCheck(RecipeIssue Issue, IReadOnlyList<RecipeRef> Recipes);
