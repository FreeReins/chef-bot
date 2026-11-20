using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Shopping;

/// <summary>A freshly worked-out shopping list, with what was left off it and why.</summary>
/// <param name="Items">What still has to be bought.</param>
/// <param name="CoveredByPantry">Needed ingredients that are already in the pantry.</param>
/// <param name="AssumedStaples">Needed ingredients treated as always on hand.</param>
public sealed record ShoppingDraft(
    IReadOnlyList<ShoppingItem> Items,
    IReadOnlyList<string> CoveredByPantry,
    IReadOnlyList<string> AssumedStaples)
{
    /// <summary>Plain sentences explaining what was left off the list.</summary>
    public IEnumerable<string> Notes()
    {
        if (CoveredByPantry.Count > 0)
        {
            yield return $"Already in your pantry: {string.Join(", ", CoveredByPantry)}.";
        }

        if (AssumedStaples.Count > 0)
        {
            yield return $"Assumed to be on hand: {string.Join(", ", AssumedStaples)}.";
        }
    }
}

/// <summary>
/// Builds the shopping list for the meals still to be cooked: totals each ingredient,
/// leaves out staples, subtracts usable pantry stock and suggests a store.
/// </summary>
public sealed class ShoppingListBuilder(IngredientCatalog catalog, StoreRouter stores)
{
    public ShoppingDraft Build(MealPlan plan, PantrySnapshot pantry)
    {
        var staples = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var covered = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var needed = new List<RequiredIngredient>();

        foreach (var requirement in MealRequirements.For(plan.Meals))
        {
            if (catalog.Get(requirement.Ingredient.CatalogName)?.PantryStaple == true)
            {
                staples.Add(requirement.Ingredient.DisplayName);
            }
            else
            {
                needed.Add(requirement);
            }
        }

        var items = new List<ShoppingItem>();
        foreach (var group in needed.GroupBy(requirement => (requirement.Ingredient.Key, requirement.Unit)))
        {
            var ingredient = group.First().Ingredient;
            var amounts = group.Where(requirement => requirement.Quantity is not null).Select(requirement => requirement.Quantity!.Value).ToList();
            decimal? wanted = amounts.Count > 0 ? amounts.Sum() : null;

            var (isCovered, toBuy) = SubtractPantry(pantry.WithKey(ingredient.Key), wanted, group.Key.Unit);
            if (isCovered)
            {
                covered.Add(ingredient.DisplayName);
                continue;
            }

            items.Add(new ShoppingItem
            {
                Key = ingredient.Key,
                CatalogName = ingredient.CatalogName,
                DisplayName = ingredient.DisplayName,
                Quantity = toBuy,
                Unit = group.Key.Unit,
                Store = stores.Route(ingredient.CatalogName, plan.WeekStart.Month),
                IsOptional = group.All(requirement => requirement.IsOptional),
            });
        }

        var ordered = items
            .OrderBy(item => item.Store, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ShoppingDraft(ordered, covered.ToList(), staples.ToList());
    }

    /// <summary>
    /// Compares what is wanted with what is on hand. When amounts can't be compared
    /// (no amount, or different units) having some is taken to be enough.
    /// </summary>
    private static (bool IsCovered, decimal? ToBuy) SubtractPantry(IReadOnlyList<PantryItem> onHand, decimal? wanted, string unit)
    {
        if (onHand.Count == 0)
        {
            return (false, wanted);
        }

        var comparable = onHand.Where(item => item.Quantity is not null && Units.Same(item.Unit, unit)).ToList();
        if (wanted is null || comparable.Count != onHand.Count)
        {
            return (true, null);
        }

        var remaining = wanted.Value - comparable.Sum(item => item.Quantity!.Value);
        return remaining <= 0m ? (true, null) : (false, remaining);
    }
}
