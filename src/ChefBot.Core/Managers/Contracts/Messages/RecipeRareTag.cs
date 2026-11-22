namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A tag only one recipe uses, often a near copy of another tag.</summary>
/// <param name="Tag">The tag, spelled as the recipe spells it.</param>
/// <param name="Recipe">The recipe that has it.</param>
public sealed record RecipeRareTag(string Tag, RecipeRef Recipe);
