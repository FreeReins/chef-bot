namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One line of a plan's shopping list.</summary>
public sealed record ShoppingItemResponse(
    Guid Id,
    string Name,
    decimal? Quantity,
    string Unit,
    string Store,
    bool IsOptional,
    bool IsPurchased,
    DateOnly? PurchasedOn);
