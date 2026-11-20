namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The household's stores, the known stores that can be added, and the ingredient names stores can list.</summary>
/// <param name="Stores">The household's stores, in the order they were saved. Empty when none are set up.</param>
/// <param name="Known">Well-known stores that can be added at any time.</param>
/// <param name="Ingredients">The catalog's ingredient names that can go on a shopping list, for choosing a store's items.</param>
public sealed record GroceryStoresResponse(
    IReadOnlyList<GroceryStoreDetails> Stores,
    IReadOnlyList<KnownGroceryStore> Known,
    IReadOnlyList<string> Ingredients);
