using ChefBot.Core.Ingredients;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Pantry;

/// <summary>Takes what a cooked meal used out of the pantry.</summary>
public static class PantryUsage
{
    /// <summary>
    /// Reduces pantry amounts, oldest first. Only items whose amount and unit can be
    /// compared with the recipe are touched; anything else is left for a person to update.
    /// </summary>
    /// <returns>The items that ran out and should be removed.</returns>
    public static IReadOnlyList<PantryItem> Deduct(IReadOnlyCollection<PantryItem> pantry, IEnumerable<RequiredIngredient> used)
    {
        var finished = new List<PantryItem>();
        foreach (var use in used)
        {
            var remaining = use.Quantity ?? 0m;
            var matches = pantry
                .Where(item => item.Quantity > 0m && item.Key == use.Ingredient.Key && Units.Same(item.Unit, use.Unit))
                .OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue)
                .ToList();

            foreach (var item in matches)
            {
                if (remaining <= 0m)
                {
                    break;
                }

                var taken = Math.Min(item.Quantity!.Value, remaining);
                item.Quantity -= taken;
                remaining -= taken;
                if (item.Quantity <= 0m)
                {
                    finished.Add(item);
                }
            }
        }

        return finished;
    }
}
