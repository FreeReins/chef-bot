using System.Text.Json.Serialization;
using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Shopping;

/// <summary>Where an ingredient the plan needs will come from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<IngredientStatus>))]
public enum IngredientStatus
{
    ToBuy,
    Purchased,
    InPantry,
    Staple,
}

/// <summary>One ingredient the plan needs, and which meals need it.</summary>
public sealed record IngredientLine(
    string Name,
    IngredientStatus Status,
    decimal? Quantity,
    string Unit,
    string? Store,
    DateOnly? ExpiresOn,
    bool IsOptional,
    IReadOnlyList<string> UsedIn,
    Guid? ShoppingItemId);

/// <summary>A pantry item, and whether the plan is going to use it.</summary>
public sealed record PantryLine(PantryItem Item, Freshness Freshness, int? DaysLeft, bool UsedByPlan);

/// <summary>Everything the plan needs next to everything on hand.</summary>
public sealed record IngredientOverview(IReadOnlyList<IngredientLine> Ingredients, IReadOnlyList<PantryLine> Pantry);

/// <summary>Builds the ingredients view: what each meal needs, where it comes from, and what the pantry holds.</summary>
public sealed class IngredientOverviewBuilder(IngredientCatalog catalog, PlannerOptions options)
{
    public IngredientOverview Build(MealPlan plan, IReadOnlyList<PantryItem> pantryItems, DateOnly today)
    {
        var pantry = new PantrySnapshot(pantryItems, today, options.ExpiringSoonDays);
        var lines = new List<IngredientLine>();
        var neededKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in MealRequirements.For(plan.Meals).GroupBy(requirement => requirement.Ingredient.Key))
        {
            neededKeys.Add(group.Key);
            lines.AddRange(LinesFor(plan, pantry, group.ToList()));
        }

        var pantryLines = pantryItems
            .OrderBy(item => item.ExpiresOn ?? DateOnly.MaxValue)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new PantryLine(
                item,
                item.FreshnessOn(today, options.ExpiringSoonDays),
                item.DaysLeft(today),
                neededKeys.Contains(item.Key)))
            .ToList();

        var ordered = lines
            .OrderBy(line => line.Status)
            .ThenBy(line => line.Store ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new IngredientOverview(ordered, pantryLines);
    }

    private IEnumerable<IngredientLine> LinesFor(MealPlan plan, PantrySnapshot pantry, IReadOnlyList<RequiredIngredient> uses)
    {
        var ingredient = uses[0].Ingredient;
        var usedIn = uses.Select(use => use.UsedIn).Distinct().ToList();
        var optional = uses.All(use => use.IsOptional);

        IngredientLine Line(string name, IngredientStatus status, decimal? quantity = null, string unit = "",
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
}
