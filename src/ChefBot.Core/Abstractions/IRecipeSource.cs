namespace ChefBot.Core.Abstractions;

/// <summary>One ingredient row as an outside source describes it.</summary>
/// <param name="FoodName">The parsed food, when the source has one.</param>
/// <param name="Quantity">The parsed amount, when the source has one.</param>
/// <param name="Unit">The parsed unit, when the source has one.</param>
/// <param name="Text">The free-text line, used when the row was never parsed into a food.</param>
public sealed record SourceIngredient(string? FoodName, decimal? Quantity, string? Unit, string? Text);

/// <summary>A recipe as an outside source describes it.</summary>
public sealed record SourceRecipe(
    string ExternalId,
    string Name,
    decimal? Servings,
    IReadOnlyList<string> Labels,
    IReadOnlyList<SourceIngredient> Ingredients);

/// <summary>Everything read from a source, and how many recipes couldn't be read.</summary>
public sealed record RecipeSourceResult(IReadOnlyList<SourceRecipe> Recipes, int Skipped);

/// <summary>An outside recipe library that can be imported, such as a Mealie instance.</summary>
public interface IRecipeSource
{
    /// <summary>False when no source has been configured.</summary>
    bool IsConfigured { get; }

    /// <exception cref="ChefBot.Core.Common.RecipeSourceException">The source couldn't be reached or read.</exception>
    Task<RecipeSourceResult> FetchAllAsync(CancellationToken cancellationToken);
}
