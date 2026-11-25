namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// One line of a plan's shopping list. <c>MoreAmounts</c> are amounts that couldn't be added to <c>Quantity</c>, and
/// <c>OrInstead</c> the general kind a variety belongs to, for finding a substitute ("onion" for shallot), or null.
/// </summary>
public sealed record ShoppingItemResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string Unit,
    string Store,
    bool IsOptional,
    bool IsPurchased,
    DateOnly? PurchasedOn,
    IReadOnlyList<AmountResponse> MoreAmounts,
    string? OrInstead);
