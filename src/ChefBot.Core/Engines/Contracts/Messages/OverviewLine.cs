using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>One ingredient the plan needs, and where it comes from.</summary>
internal sealed record OverviewLine(
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
