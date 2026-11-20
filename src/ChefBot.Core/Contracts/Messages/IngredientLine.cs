namespace ChefBot.Core.Contracts.Messages;

/// <summary>One ingredient the plan needs, and which meals need it.</summary>
public sealed record IngredientLine(
    string Name,
    IngredientStatus Status,
    decimal? Quantity,
    string Unit,
    string? Store,
    DateOnly? ExpiresOn,
    bool IsOptional,
    IReadOnlyList<string> UsedIn,
    Guid? ShoppingItemId);
