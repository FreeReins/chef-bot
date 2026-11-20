using ChefBot.Core.Ingredients;

namespace ChefBot.Core.Shopping;

/// <summary>Where the household shops. Bound from the "Stores" settings section.</summary>
public sealed class StoreOptions
{
    /// <summary>The store for anything without a better match.</summary>
    public string Default { get; set; } = "Grocery store";

    /// <summary>Where in-season produce is sent during market months. Empty turns this off.</summary>
    public string FarmersMarket { get; set; } = string.Empty;

    /// <summary>Months (1-12) the farmers market is worth a trip.</summary>
    public List<int> FarmersMarketMonths { get; set; } = [];
}

/// <summary>Makes a first guess at which store each ingredient comes from.</summary>
public sealed class StoreRouter(StoreOptions options, IngredientCatalog catalog)
{
    public string Route(string? catalogName, int month)
    {
        var entry = catalog.Get(catalogName);
        if (!string.IsNullOrWhiteSpace(entry?.Store))
        {
            return entry.Store;
        }

        var marketIsOpen = options.FarmersMarket.Length > 0 && options.FarmersMarketMonths.Contains(month);
        return marketIsOpen && entry is { IsFreshProduce: true } && entry.IsInSeason(month) == true
            ? options.FarmersMarket
            : options.Default;
    }
}
