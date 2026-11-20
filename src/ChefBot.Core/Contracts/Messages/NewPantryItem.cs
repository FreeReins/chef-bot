namespace ChefBot.Core.Contracts.Messages;

/// <summary>Something added to the pantry by hand.</summary>
public sealed record NewPantryItem(
    string Name,
    decimal? Quantity = null,
    string? Unit = null,
    DateOnly? PurchasedOn = null,
    DateOnly? ExpiresOn = null);
