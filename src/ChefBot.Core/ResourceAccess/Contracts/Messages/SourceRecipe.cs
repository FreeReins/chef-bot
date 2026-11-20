namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A recipe as an outside source describes it.</summary>
internal sealed record SourceRecipe(
    string ExternalId,
    string Name,
    decimal? Servings,
    IReadOnlyList<string> Labels,
    IReadOnlyList<SourceIngredient> Ingredients);
