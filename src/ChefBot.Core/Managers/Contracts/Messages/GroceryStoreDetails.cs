namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// A store the household shops at and what is bought there. Shopping items go to the first store that lists them,
/// then in-season produce to a seasonal store during its months, then everything else to the main store.
/// </summary>
/// <param name="Name">What the store is called on the shopping list.</param>
/// <param name="IsMain">True for the store that gets anything not sent elsewhere. At most one store is the main one.</param>
/// <param name="Items">Ingredient names always bought here, such as "chicken" or "daikon".</param>
/// <param name="SeasonalProduce">True when in-season fruit, vegetables and herbs are bought here during <paramref name="Months"/>.</param>
/// <param name="Months">The months (1-12) a seasonal store is open.</param>
public sealed record GroceryStoreDetails(
    string Name,
    bool IsMain,
    IReadOnlyList<string>? Items,
    bool SeasonalProduce,
    IReadOnlyList<int>? Months);
