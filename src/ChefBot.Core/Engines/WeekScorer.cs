using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Scores a set of recipes as a week: higher when they share perishables, suit the season, use up what is in the
/// pantry and are the household's favourites, lower when they lean on one protein. Planning uses it to choose a week,
/// and swaps use it to rank single recipes.
/// </summary>
/// <param name="catalog">What is known about each ingredient.</param>
/// <param name="options">The scoring weights.</param>
/// <param name="pantry">What is on hand.</param>
/// <param name="month">The month being planned, for seasons.</param>
/// <param name="pantryBoost">Multiplies the reward for using pantry food. Above 1 when a plan is rebuilt around what was bought.</param>
/// <param name="fans">How many people have each recipe as a favourite.</param>
/// <param name="favouriteWeight">What a recipe with one fan adds; each further fan adds less (1, 1/2, 1/3, ...). 0 ignores favourites.</param>
internal sealed class WeekScorer(
    IngredientCatalog catalog,
    PlannerOptions options,
    PantrySnapshot pantry,
    int month,
    double pantryBoost = 1,
    IReadOnlyDictionary<Guid, int>? fans = null,
    double favouriteWeight = 0)
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

        // More fans count for more, with each further fan adding less, so a recipe everyone loves comes ahead of one
        // person's pick without crowding everyone else's favourites out.
        for (var fan = 1; fan <= (fans?.GetValueOrDefault(recipe.Id) ?? 0); fan++)
        {
            score += favouriteWeight / fan;
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
