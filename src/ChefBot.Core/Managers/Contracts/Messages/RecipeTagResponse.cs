namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A tag used in the recipe library.</summary>
/// <param name="Name">The tag, spelled as most recipes spell it.</param>
/// <param name="Recipes">How many recipes that can be planned have it.</param>
public sealed record RecipeTagResponse(string Name, int Recipes);
