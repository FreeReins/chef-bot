using ChefBot.Core.Common;
using ChefBot.Core.Ingredients;

namespace ChefBot.Core.Pantry;

/// <summary>Creates pantry items and estimates their expiration dates from the catalog.</summary>
public sealed class PantryItemFactory(IngredientCatalog catalog)
{
    /// <param name="ingredient">What was bought.</param>
    /// <param name="quantity">How much, when known.</param>
    /// <param name="unit">The unit of <paramref name="quantity"/>.</param>
    /// <param name="purchasedOn">The purchase date.</param>
    /// <param name="expiresOn">The date on the package. When null, the catalog's shelf life is used.</param>
    public PantryItem Create(IIngredientRef ingredient, decimal? quantity, string? unit, DateOnly purchasedOn, DateOnly? expiresOn)
    {
        if (quantity is <= 0m)
        {
            throw new RuleViolationException("A quantity must be more than zero.");
        }

        if (expiresOn < purchasedOn)
        {
            throw new RuleViolationException("The expiration date can't be before the purchase date.");
        }

        var shelfLife = catalog.Get(ingredient.CatalogName)?.ShelfLifeDays;
        DateOnly? estimated = shelfLife is int days ? purchasedOn.AddDays(days) : null;

        return new PantryItem
        {
            Key = ingredient.Key,
            CatalogName = ingredient.CatalogName,
            DisplayName = ingredient.DisplayName,
            Quantity = quantity,
            Unit = Units.Normalize(unit),
            PurchasedOn = purchasedOn,
            ExpiresOn = expiresOn ?? estimated,
        };
    }
}
