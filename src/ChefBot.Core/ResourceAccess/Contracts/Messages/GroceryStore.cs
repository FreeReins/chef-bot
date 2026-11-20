namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A store the household shops at, and what is bought there.</summary>
internal sealed class GroceryStore
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    /// <summary>True for the store that gets anything not sent elsewhere.</summary>
    public bool IsMain { get; set; }

    /// <summary>Where it comes in the household's list of stores.</summary>
    public int Position { get; set; }

    /// <summary>Ingredient catalog names, or ingredient keys for things not in the catalog, always bought here.</summary>
    public List<string> Items { get; set; } = [];

    /// <summary>True when in-season produce is bought here during <see cref="Months"/>.</summary>
    public bool SeasonalProduce { get; set; }

    /// <summary>Months (1-12) a seasonal store is open.</summary>
    public List<int> Months { get; set; } = [];

    /// <summary>True when <see cref="Items"/> lists the ingredient by its catalog name or its exact key.</summary>
    public bool Sells(string? catalogName, string key) =>
        Items.Contains(key, StringComparer.OrdinalIgnoreCase)
        || (catalogName is not null && Items.Contains(catalogName, StringComparer.OrdinalIgnoreCase));
}
