namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// One ingredient the plan needs, and which meals need it. <c>Store</c> is empty when no store was chosen for it;
/// <c>Group</c> is its food group, which the shopping list uses instead of a store then.
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
    Guid? ShoppingItemId);
