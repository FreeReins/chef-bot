namespace ChefBot.Core.Utilities;

/// <summary>Where the household shops. Bound from the "Stores" settings section.</summary>
internal sealed class StoreOptions
{
    /// <summary>The store for anything without a better match.</summary>
    public string Default { get; set; } = "Grocery store";

    /// <summary>Where in-season produce is sent during market months. Empty turns this off.</summary>
    public string FarmersMarket { get; set; } = string.Empty;

    /// <summary>Months (1-12) the farmers market is worth a trip.</summary>
    public List<int> FarmersMarketMonths { get; set; } = [];
}
