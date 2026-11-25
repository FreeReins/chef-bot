namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class ShoppingItemEntity
{
    public Guid Id { get; set; }

    public Guid MealPlanId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>Amounts that couldn't be added to <see cref="Quantity"/>, as a JSON list of quantity and unit.</summary>
    public string ExtraAmounts { get; set; } = "[]";

    public string Store { get; set; } = string.Empty;

    public bool IsOptional { get; set; }

    public bool IsPurchased { get; set; }

    public DateOnly? PurchasedOn { get; set; }
}
