namespace ChefBot.Core.Ingredients;

/// <summary>What ChefBot knows about one ingredient. Entries are loaded from config/ingredients.json.</summary>
public sealed record CatalogEntry
{
    /// <summary>The shared name every alias maps to, for example "green onion".</summary>
    public required string Name { get; init; }

    /// <summary>Other names for the same ingredient, for example "scallion".</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    public IngredientGroup Group { get; init; } = IngredientGroup.Other;

    /// <summary>Roughly how long it keeps after purchase. Used to estimate expiration dates.</summary>
    public int? ShelfLifeDays { get; init; }

    /// <summary>Months (1-12) it is in season locally. Empty means it is never scored for season.</summary>
    public IReadOnlyList<int> SeasonMonths { get; init; } = [];

    /// <summary>True for things assumed to be on hand, which never go on a shopping list.</summary>
    public bool PantryStaple { get; init; }

    /// <summary>
    /// True when the exact variety matters at the store, so "butternut squash"
    /// stays on the list under that name while still matching "winter squash" for planning.
    /// </summary>
    public bool KeepSpecificName { get; init; }

    /// <summary>The store that usually has it, when that isn't the default store.</summary>
    public string? Store { get; init; }

    public bool IsFreshProduce => Group is IngredientGroup.Vegetable or IngredientGroup.Fruit or IngredientGroup.Herb;

    /// <summary>True, false, or null when the ingredient has no season.</summary>
    public bool? IsInSeason(int month) => SeasonMonths.Count == 0 ? null : SeasonMonths.Contains(month);

    public bool SpoilsWithin(int days) => ShelfLifeDays is int shelfLife && shelfLife <= days;
}
