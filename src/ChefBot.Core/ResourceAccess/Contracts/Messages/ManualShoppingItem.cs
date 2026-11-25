using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// Something put on the shopping list by hand rather than by a plan. It stays until it is bought or removed,
/// whatever happens to plans. Where to buy it is worked out from the stores each time it is shown.
/// </summary>
internal sealed class ManualShoppingItem : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How much to buy, or null when it wasn't said.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>Amounts that couldn't be added to <see cref="Quantity"/>, such as 100 g beside 2 tbsp.</summary>
    public List<ShoppingAmount> ExtraAmounts { get; set; } = [];

    public DateOnly AddedOn { get; set; }
}
