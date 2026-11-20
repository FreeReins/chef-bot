using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Works out shopping and stock: the shopping list for the meals still to be cooked, the ingredients view, and
/// what cooking a meal takes out of the pantry. Reads and saves nothing.
/// </summary>
internal sealed class ShoppingEngine(IngredientCatalog catalog, StoreOptions stores)
{
    /// <summary>Totals each ingredient, leaves out staples, subtracts usable pantry stock and suggests a store.</summary>
    public ShoppingDraft BuildList(MealPlan plan, PantrySnapshot pantry)
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
                Store = StoreFor(ingredient.CatalogName, plan.WeekStart.Month),
                IsOptional = group.All(requirement => requirement.IsOptional),
            });
        }

        var ordered = items
            .OrderBy(item => item.Store, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ShoppingDraft(ordered, covered.ToList(), staples.ToList());
    }

    /// <summary>Replaces everything still to buy with the new list. Purchased items are history and stay.</summary>
    public void ApplyList(MealPlan plan, ShoppingDraft draft)
    {
        plan.ShoppingItems.RemoveAll(item => !item.IsPurchased);
        plan.ShoppingItems.AddRange(draft.Items);
    }

    /// <summary>A first guess at which store an ingredient comes from.</summary>
    public string StoreFor(string? catalogName, int month)
    {
        var entry = catalog.Get(catalogName);
        if (!string.IsNullOrWhiteSpace(entry?.Store))
        {
            return entry.Store;
        }

        var marketIsOpen = stores.FarmersMarket.Length > 0 && stores.FarmersMarketMonths.Contains(month);
        return marketIsOpen && entry is { IsFreshProduce: true } && entry.IsInSeason(month) == true
            ? stores.FarmersMarket
            : stores.Default;
    }

    /// <summary>The ingredients view: what each meal needs, where it comes from, and what the pantry holds.</summary>
    public IngredientOverview BuildOverview(MealPlan plan, IReadOnlyList<PantryItem> pantryItems, DateOnly today, int expiringSoonDays)
    {
        var pantry = new PantrySnapshot(pantryItems, today, expiringSoonDays);
        var lines = new List<OverviewLine>();
        var neededKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in MealRequirements.For(plan.Meals).GroupBy(requirement => requirement.Ingredient.Key))
        {
            neededKeys.Add(group.Key);
            lines.AddRange(LinesFor(plan, pantry, group.ToList()));
        }

        var pantryLines = pantryItems
            .OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new PantryLine(item, neededKeys.Contains(item.Key)))
            .ToList();

        var ordered = lines
            .OrderBy(line => line.Status)
            .ThenBy(line => line.Store ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new IngredientOverview(ordered, pantryLines);
    }

    /// <summary>
    /// Takes what a cooked meal used out of the pantry, oldest first. Only items whose amount and unit can be
    /// compared with the recipe are touched; anything else is left for a person to update. Changes the
    /// quantities of <paramref name="pantry"/> in place.
    /// </summary>
    /// <returns>The items that ran out and should be removed.</returns>
    public IReadOnlyList<PantryItem> DeductCooked(IReadOnlyCollection<PantryItem> pantry, PlannedMeal meal)
    {
        var finished = new List<PantryItem>();
        foreach (var use in MealRequirements.For([meal]))
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

    private IEnumerable<OverviewLine> LinesFor(MealPlan plan, PantrySnapshot pantry, IReadOnlyList<RequiredIngredient> uses)
    {
        var ingredient = uses[0].Ingredient;
        var usedIn = uses.Select(use => use.UsedIn).Distinct().ToList();
        var optional = uses.All(use => use.IsOptional);

        OverviewLine Line(string name, IngredientStatus status, decimal? quantity = null, string unit = "",
            string? store = null, DateOnly? expiresOn = null, Guid? shoppingItemId = null) =>
            new(name, status, quantity, unit, store, expiresOn, optional, usedIn, shoppingItemId);

        if (catalog.Get(ingredient.CatalogName)?.PantryStaple == true)
        {
            yield return Line(ingredient.DisplayName, IngredientStatus.Staple);
            yield break;
        }

        var listed = plan.ShoppingItems.Where(item => item.Key == ingredient.Key).ToList();
        var open = listed.Where(item => !item.IsPurchased).ToList();
        foreach (var item in open)
        {
            yield return Line(item.DisplayName, IngredientStatus.ToBuy, item.Quantity, item.Unit, item.Store, shoppingItemId: item.Id);
        }

        var onHand = pantry.WithKey(ingredient.Key);
        if (onHand.Count > 0)
        {
            yield return Line(ingredient.DisplayName, IngredientStatus.InPantry, expiresOn: onHand.Min(item => item.ExpiresOn));
        }
        else if (open.Count == 0 && listed.Count > 0)
        {
            // Bought for this plan, but no longer in the pantry.
            yield return Line(ingredient.DisplayName, IngredientStatus.Purchased);
        }
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
