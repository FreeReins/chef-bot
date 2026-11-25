namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class ManualShoppingItemEntity
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>Amounts that couldn't be added to <see cref="Quantity"/>, as a JSON list of quantity and unit.</summary>
    public string ExtraAmounts { get; set; } = "[]";

    public DateOnly AddedOn { get; set; }
}
