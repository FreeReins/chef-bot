namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Something put on the shopping list by hand. It stays there until it is bought or removed.</summary>
/// <param name="Id">The item's id.</param>
/// <param name="Name">The ingredient's name.</param>
/// <param name="Quantity">How much to buy, or null when it wasn't said.</param>
/// <param name="Unit">The unit <paramref name="Quantity"/> is measured in.</param>
/// <param name="Store">Where to buy it, chosen from the household's stores. Empty when no store applies.</param>
/// <param name="Group">The ingredient's food group.</param>
/// <param name="Area">The part of the store it is found in, which the list groups by.</param>
/// <param name="MoreAmounts">Amounts that couldn't be added to <paramref name="Quantity"/>, such as 100 g beside 2 tbsp.</param>
/// <param name="OrInstead">The general kind a variety belongs to, for finding a substitute ("onion" for shallot), or null.</param>
public sealed record ManualShoppingItemResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string Unit,
    string Store,
    IngredientGroup Group,
    StoreArea Area,
    IReadOnlyList<AmountResponse> MoreAmounts,
    string? OrInstead);
