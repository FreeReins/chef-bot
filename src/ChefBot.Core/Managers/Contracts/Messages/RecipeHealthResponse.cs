namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How complete the recipe library is: what each recipe is missing, and names and tags to tidy up.</summary>
/// <param name="Recipes">How many recipes can be planned.</param>
/// <param name="Complete">How many of them have no issues.</param>
/// <param name="Checks">Every check, in a fixed order, each with the recipes that fail it.</param>
/// <param name="Duplicates">Groups of recipes that share a name.</param>
/// <param name="RareTags">Tags only one recipe uses, often a near copy of another tag.</param>
public sealed record RecipeHealthResponse(
    int Recipes,
    int Complete,
    IReadOnlyList<RecipeHealthCheck> Checks,
    IReadOnlyList<RecipeDuplicateGroup> Duplicates,
    IReadOnlyList<RecipeRareTag> RareTags);
