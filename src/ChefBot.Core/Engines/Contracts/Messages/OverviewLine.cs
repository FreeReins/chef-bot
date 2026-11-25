using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>
/// One ingredient the plan needs, and where it comes from. <c>ExtraAmounts</c> are amounts that couldn't be added to
/// <c>Quantity</c>; <c>OrInstead</c> is the general kind it belongs to, for finding a substitute.
/// </summary>
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
    Guid? ShoppingItemId,
    StoreArea Area,
    IReadOnlyList<ShoppingAmount> ExtraAmounts,
    string? OrInstead);
