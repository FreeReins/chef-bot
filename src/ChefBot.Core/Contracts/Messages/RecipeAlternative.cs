namespace ChefBot.Core.Contracts.Messages;

/// <summary>A recipe that could replace a planned meal, and what choosing it would mean.</summary>
/// <param name="RecipeId">The recipe to swap in.</param>
/// <param name="Name">Its name.</param>
/// <param name="IsSlowCooker">True for slow cooker recipes.</param>
/// <param name="Score">Higher is a better fit for what is on hand and in season.</param>
/// <param name="OnHand">Ingredients already in the pantry.</param>
/// <param name="ExpiringSoon">Pantry ingredients it would use before they expire.</param>
/// <param name="ToBuy">Ingredients it would add to the shopping list.</param>
public sealed record RecipeAlternative(
    Guid RecipeId,
    string Name,
    bool IsSlowCooker,
    double Score,
    IReadOnlyList<string> OnHand,
    IReadOnlyList<string> ExpiringSoon,
    IReadOnlyList<string> ToBuy);
