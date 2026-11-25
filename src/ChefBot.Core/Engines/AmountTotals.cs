using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Adds up the amounts of one ingredient so it takes one line on a shopping list. Amounts that measure the same thing
/// are added; measured amounts of an item the catalog knows the size of become whole items (2 tbsp of lemon juice is
/// a lemon); whatever is left is kept beside the main amount.
/// </summary>
internal static class AmountTotals
{
    /// <summary>
    /// The total of <paramref name="amounts"/>: the main amount, and the ones that couldn't be added to it. Lines
    /// without an amount count only when none has one. Whole items are rounded up, since nobody buys half a lemon.
    /// </summary>
    public static (decimal? Quantity, string Unit, List<ShoppingAmount> Extra) Combine(
        CatalogEntry? entry, IIngredientRef ingredient, IReadOnlyList<(decimal? Quantity, string Unit)> amounts)
    {
        var known = amounts.Where(amount => amount.Quantity is not null).ToList();
        if (known.Count == 0)
        {
            return (null, Units.Normalize(amounts.FirstOrDefault().Unit), []);
        }

        var size = SizeOf(entry, ingredient);
        decimal? count = null;
        var totals = new List<(string Unit, decimal Quantity)>();
        foreach (var (quantity, written) in known)
        {
            var unit = Units.Normalize(written);
            if (size is not null && ToCount(size, quantity!.Value, unit) is decimal items)
            {
                count = (count ?? 0m) + items;
                continue;
            }

            var index = totals.FindIndex(total => Units.Comparable(total.Unit, unit));
            if (index < 0)
            {
                totals.Add((unit, quantity!.Value));
            }
            else if (Units.TryConvert(quantity!.Value, unit, totals[index].Unit, out var converted))
            {
                totals[index] = (totals[index].Unit, totals[index].Quantity + converted);
            }
        }

        var all = new List<ShoppingAmount>();
        if (count is decimal whole)
        {
            all.Add(new ShoppingAmount(Math.Ceiling(whole), Units.Normalize(size!.CountUnit)));
        }

        all.AddRange(totals.Select(total => Units.Readable(total.Quantity, total.Unit)).Select(readable => new ShoppingAmount(readable.Quantity, readable.Unit)));
        return (all[0].Quantity, all[0].Unit, all.Skip(1).ToList());
    }

    /// <summary>
    /// <paramref name="quantity"/> re-expressed in <paramref name="target"/>, going through whole items when the catalog
    /// knows their size: 6 tbsp of lemon is 2 lemons. False when the units can't be compared.
    /// </summary>
    public static bool TryConvert(CatalogEntry? entry, IIngredientRef ingredient, decimal quantity, string unit, string target, out decimal result)
    {
        if (Units.TryConvert(quantity, unit, target, out result))
        {
            return true;
        }

        if (SizeOf(entry, ingredient) is { } size)
        {
            if (Units.Same(target, size.CountUnit) && ToCount(size, quantity, Units.Normalize(unit)) is decimal items)
            {
                result = items;
                return true;
            }

            if (Units.Same(unit, size.CountUnit) && Units.TryConvert(quantity * size.Quantity, size.Unit, target, out result))
            {
                return true;
            }
        }

        result = 0m;
        return false;
    }

    /// <summary>
    /// The size of one item, when the catalog knows it and the ingredient is the entry itself: a shallot is not
    /// measured by an onion's size.
    /// </summary>
    private static CatalogSize? SizeOf(CatalogEntry? entry, IIngredientRef ingredient) =>
        entry?.Each is { Quantity: > 0m } size && string.Equals(ingredient.Key, entry.Name, StringComparison.Ordinal) ? size : null;

    /// <summary>How many whole items an amount is, or null when it is in a unit the size can't be compared with.</summary>
    private static decimal? ToCount(CatalogSize size, decimal quantity, string unit)
    {
        if (Units.Same(unit, size.CountUnit))
        {
            return quantity;
        }

        return Units.TryConvert(quantity, unit, size.Unit, out var measured) ? measured / size.Quantity : null;
    }
}
