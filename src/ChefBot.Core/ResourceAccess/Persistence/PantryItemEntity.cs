namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class PantryItemEntity
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateOnly PurchasedOn { get; set; }

    public DateOnly? ExpiresOn { get; set; }
}
