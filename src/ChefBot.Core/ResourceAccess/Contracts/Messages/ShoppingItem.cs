using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One line of a plan's shopping list.</summary>
internal sealed class ShoppingItem : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How much to buy, or null when the recipes don't say.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>A first guess at where to buy it.</summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>True when only optional meals need it.</summary>
    public bool IsOptional { get; set; }

    public bool IsPurchased { get; set; }

    public DateOnly? PurchasedOn { get; set; }
}
