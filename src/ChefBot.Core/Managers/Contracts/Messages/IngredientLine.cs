namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// One ingredient the plan needs, and which meals need it. <c>Store</c> is empty when no store was chosen for it;
/// <c>Group</c> is its food group, and <c>Area</c> the part of the store it is found in,
/// which the shopping list groups by within a store. <c>MoreAmounts</c> are amounts that couldn't be added to
/// <c>Quantity</c> ("2 tbsp + 100 g"), and <c>OrInstead</c> the general kind a variety belongs to, for finding a
/// substitute ("onion" for shallot), or null.
/// </summary>
public sealed record IngredientLine(
    string Name,
    IngredientStatus Status,
    decimal? Quantity,
    string Unit,
    string? Store,
    IngredientGroup Group,
    DateOnly? ExpiresOn,
    bool IsOptional,
    IReadOnlyList<string> UsedIn,
    Guid? ShoppingItemId,
    StoreArea Area,
    IReadOnlyList<AmountResponse> MoreAmounts,
    string? OrInstead);
