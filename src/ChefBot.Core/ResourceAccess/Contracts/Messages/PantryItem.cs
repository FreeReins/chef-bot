using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Something in the kitchen: what it is, how much, when it was bought and when it expires.</summary>
internal sealed class PantryItem : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How much is left, or null when the amount isn't tracked.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateOnly PurchasedOn { get; set; }

    /// <summary>The expiration date, estimated from the catalog's shelf life unless one was entered.</summary>
    public DateOnly? ExpiresOn { get; set; }
}
