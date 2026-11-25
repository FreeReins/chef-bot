namespace ChefBot.Core.Utilities;

/// <summary>How much one whole item of an ingredient gives: 3 tbsp for a lemon, 1 tsp for a clove of garlic.</summary>
/// <param name="Quantity">The amount one item gives.</param>
/// <param name="Unit">The unit of <paramref name="Quantity"/>.</param>
/// <param name="CountUnit">What one item is counted in: "clove" for garlic, empty for a whole lemon.</param>
internal sealed record CatalogSize(decimal Quantity, string Unit, string CountUnit = "");
