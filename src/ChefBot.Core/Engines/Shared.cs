using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

// Calculations several engines need. They are plain functions and value types, not engines, so using them doesn't
// make one engine call another.

/// <summary>How fresh a pantry item is on a given day.</summary>
internal static class PantryFreshness
{
    public static bool IsExpired(this PantryItem item, DateOnly today) => item.ExpiresOn is DateOnly date && date < today;

    /// <summary>Days until it expires. Negative once it has, null when no date is known.</summary>
    public static int? DaysLeft(this PantryItem item, DateOnly today) =>
        item.ExpiresOn is DateOnly date ? date.DayNumber - today.DayNumber : null;

    public static Freshness FreshnessOn(this PantryItem item, DateOnly today, int expiringSoonDays) => item.DaysLeft(today) switch
    {
        null => Freshness.Unknown,
        < 0 => Freshness.Expired,
        var days when days <= expiringSoonDays => Freshness.ExpiringSoon,
        _ => Freshness.Fresh,
    };
}

/// <summary>
/// A read-only view of the pantry on one day. Expired items are left out,
/// so planning and shopping never count on food that has gone off.
/// </summary>
internal sealed class PantrySnapshot
{
    private readonly ILookup<string, PantryItem> _byKey;
    private readonly Dictionary<string, DateOnly?> _earliestExpiry = new(StringComparer.Ordinal);
    private readonly DateOnly _today;
    private readonly int _expiringSoonDays;

    public PantrySnapshot(IEnumerable<PantryItem> items, DateOnly today, int expiringSoonDays)
    {
        _today = today;
        _expiringSoonDays = expiringSoonDays;
        Usable = items.Where(item => !item.IsExpired(today)).ToList();
        _byKey = Usable.ToLookup(item => item.Key, StringComparer.Ordinal);

        foreach (var item in Usable)
        {
            var name = item.MatchName();
            if (!_earliestExpiry.TryGetValue(name, out var known) || IsEarlier(item.ExpiresOn, known))
            {
                _earliestExpiry[name] = item.ExpiresOn;
            }
        }
    }

    public static PantrySnapshot Empty { get; } = new PantrySnapshot([], DateOnly.MinValue, 0);

    /// <summary>Everything that hasn't expired.</summary>
    public IReadOnlyList<PantryItem> Usable { get; }

    /// <summary>True when something matching this name is on hand. See <see cref="IngredientRefExtensions.MatchName"/>.</summary>
    public bool Has(string matchName) => _earliestExpiry.ContainsKey(matchName);

    /// <summary>The soonest expiration date among matching items, if any has one.</summary>
    public DateOnly? ExpiryOf(string matchName) => _earliestExpiry.GetValueOrDefault(matchName);

    public int? DaysLeft(string matchName) =>
        ExpiryOf(matchName) is DateOnly date ? date.DayNumber - _today.DayNumber : null;

    public bool ExpiresSoon(string matchName) => DaysLeft(matchName) is int days && days <= _expiringSoonDays;

    /// <summary>Usable items with exactly this key, used when amounts matter.</summary>
    public IReadOnlyList<PantryItem> WithKey(string key) => _byKey[key].ToList();

    private static bool IsEarlier(DateOnly? candidate, DateOnly? known) =>
        candidate is DateOnly date && (known is not DateOnly existing || date < existing);
}

/// <summary>An ingredient a plan needs, scaled to the portions being cooked.</summary>
/// <param name="Ingredient">What is needed.</param>
/// <param name="Quantity">How much, or null when the recipe doesn't say.</param>
/// <param name="Unit">The unit of <paramref name="Quantity"/>.</param>
/// <param name="IsOptional">True when only optional meals need it.</param>
/// <param name="UsedIn">The meal that needs it.</param>
internal sealed record RequiredIngredient(IIngredientRef Ingredient, decimal? Quantity, string Unit, bool IsOptional, string UsedIn);

/// <summary>Works out what a set of meals needs. Shared by the shopping list, the ingredients view, cooking and reminders.</summary>
internal static class MealRequirements
{
    /// <summary>Ingredients for the meals still to be cooked. Leftovers need nothing: their dinner covers them.</summary>
    public static IReadOnlyList<RequiredIngredient> For(IEnumerable<PlannedMeal> meals)
    {
        var toCook = meals.Where(meal => meal.Status == MealStatus.Planned && !meal.IsLeftover).ToList();
        var required = new List<RequiredIngredient>();

        // A recipe planned more than once is scaled once, from its total portions.
        foreach (var batch in toCook.Where(meal => meal.Recipe is not null).GroupBy(meal => meal.Recipe!.Id))
        {
            var recipe = batch.First().Recipe!;
            var scale = Scale(batch.ToList(), recipe.Servings);
            var optional = batch.All(meal => meal.IsOptional);
            required.AddRange(recipe.Ingredients
                .OrderBy(ingredient => ingredient.Position)
                .Select(ingredient => new RequiredIngredient(ingredient, ingredient.Quantity * scale, ingredient.Unit, optional, recipe.Name)));
        }

        foreach (var meal in toCook.Where(m => m.Recipe is null))
        {
            required.AddRange(meal.CustomIngredients
                .Select(ingredient => new RequiredIngredient(ingredient, null, string.Empty, meal.IsOptional, meal.Title)));
        }

        return required;
    }

    /// <summary>
    /// How many times to multiply a recipe, rounded up to the nearest half batch.
    /// Meals with zero portions count as one batch each.
    /// </summary>
    public static decimal Scale(IReadOnlyCollection<PlannedMeal> meals, decimal servings)
    {
        var portions = meals.Sum(meal => meal.Portions);
        if (portions <= 0)
        {
            return meals.Count;
        }

        return Math.Ceiling(portions / Math.Max(servings, 1m) * 2m) / 2m;
    }
}

/// <summary>
/// Scores a set of recipes as a week: higher when they share perishables, suit the season
/// and use up what is in the pantry, lower when they lean on one protein. Planning uses it to choose a week,
/// and swaps use it to rank single recipes.
/// </summary>
/// <param name="catalog">What is known about each ingredient.</param>
/// <param name="options">The scoring weights.</param>
/// <param name="pantry">What is on hand.</param>
/// <param name="month">The month being planned, for seasons.</param>
/// <param name="pantryBoost">Multiplies the reward for using pantry food. Above 1 when a plan is rebuilt around what was bought.</param>
internal sealed class WeekScorer(IngredientCatalog catalog, PlannerOptions options, PantrySnapshot pantry, int month, double pantryBoost = 1)
{
    private readonly Dictionary<Guid, Profile> _profiles = new();

    /// <summary>The part of a recipe's score that doesn't depend on the other meals.</summary>
    public double ScoreAlone(Recipe recipe) => ProfileOf(recipe).Score;

    public double Score(IEnumerable<Recipe> meals)
    {
        var weights = options.Weights;
        var perishables = new Dictionary<string, int>(StringComparer.Ordinal);
        var proteins = new Dictionary<string, int>(StringComparer.Ordinal);
        var total = 0d;

        foreach (var meal in meals)
        {
            var profile = ProfileOf(meal);
            total += profile.Score;
            Count(perishables, profile.Perishables);
            Count(proteins, profile.Proteins);
        }

        foreach (var (name, uses) in perishables)
        {
            if (uses > 1)
            {
                total += weights.SharedPerishable * (uses - 1);
            }
            else if (!pantry.Has(name))
            {
                total += weights.StrandedPerishable;
            }
        }

        foreach (var uses in proteins.Values.Where(uses => uses > 2))
        {
            total += weights.SameProtein * (uses - 2);
        }

        return total;
    }

    private Profile ProfileOf(Recipe recipe)
    {
        if (_profiles.TryGetValue(recipe.Id, out var cached))
        {
            return cached;
        }

        var weights = options.Weights;
        var entries = recipe.Ingredients
            .Where(ingredient => ingredient.CatalogName is not null)
            .Select(ingredient => ingredient.CatalogName!)
            .Distinct()
            .Select(name => catalog.Get(name))
            .OfType<CatalogEntry>()
            .Where(entry => !entry.PantryStaple)
            .ToList();

        var inSeason = entries.Count(entry => entry.IsInSeason(month) == true);
        var outOfSeason = entries.Count(entry => entry.IsInSeason(month) == false);
        var score = weights.Seasonal * (inSeason - outOfSeason) / Math.Max(1, inSeason + outOfSeason);

        foreach (var entry in entries.Where(entry => pantry.Has(entry.Name)))
        {
            score += weights.OnHand * pantryBoost;
            if (pantry.ExpiresSoon(entry.Name))
            {
                score += weights.ExpiringSoon;
            }
        }

        var profile = new Profile(
            score,
            entries.Where(entry => entry.SpoilsWithin(options.PerishableWithinDays)).Select(entry => entry.Name).ToList(),
            entries.Where(entry => entry.Group == IngredientGroup.Protein).Select(entry => entry.Name).ToList());

        _profiles[recipe.Id] = profile;
        return profile;
    }

    private static void Count(Dictionary<string, int> counts, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }
    }

    private sealed record Profile(double Score, IReadOnlyList<string> Perishables, IReadOnlyList<string> Proteins);
}
