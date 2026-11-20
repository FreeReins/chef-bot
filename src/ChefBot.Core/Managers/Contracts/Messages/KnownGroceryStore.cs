namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// A well-known store or kind of store, offered as a starting point when choosing where to shop. Choosing one copies
/// its name and suggested items into the household's own stores, where they can be changed.
/// </summary>
/// <param name="Name">The store's name.</param>
/// <param name="Kind">What sort of store it is.</param>
/// <param name="Description">Where it is found or what it is good for, in a few words.</param>
/// <param name="Items">Ingredient names usually bought there instead of at the main store.</param>
/// <param name="SeasonalProduce">True for markets that sell in-season produce during <paramref name="Months"/>.</param>
/// <param name="Months">The months (1-12) a seasonal market is usually open.</param>
public sealed record KnownGroceryStore(
    string Name,
    GroceryStoreKind Kind,
    string Description,
    IReadOnlyList<string> Items,
    bool SeasonalProduce,
    IReadOnlyList<int> Months);
