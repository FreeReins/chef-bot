using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One ingredient line of a recipe.</summary>
internal sealed class RecipeIngredient : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The line's place in the recipe.</summary>
    public int Position { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The amount for the recipe as written, or null when the recipe doesn't say.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>The line as the recipe writes it: "2 cups kale, stems removed". Empty for rows saved before it was kept.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Extra words the source keeps apart from the food, such as "stems removed".</summary>
    public string? Note { get; set; }
}
