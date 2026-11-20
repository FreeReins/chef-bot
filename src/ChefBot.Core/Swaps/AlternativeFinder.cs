using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Swaps;

/// <summary>A recipe that could replace a planned meal, and what choosing it would mean.</summary>
/// <param name="RecipeId">The recipe to swap in.</param>
/// <param name="Name">Its name.</param>
/// <param name="IsSlowCooker">True for slow cooker recipes.</param>
/// <param name="Score">Higher is a better fit for what is on hand and in season.</param>
/// <param name="OnHand">Ingredients already in the pantry.</param>
/// <param name="ExpiringSoon">Pantry ingredients it would use before they expire.</param>
/// <param name="ToBuy">Ingredients it would add to the shopping list.</param>
public sealed record RecipeAlternative(
    Guid RecipeId,
    string Name,
    bool IsSlowCooker,
    double Score,
    IReadOnlyList<string> OnHand,
    IReadOnlyList<string> ExpiringSoon,
    IReadOnlyList<string> ToBuy);

/// <summary>Ranks recipes by how well they use the pantry, especially food that is about to expire.</summary>
public sealed class AlternativeFinder(IngredientCatalog catalog, PlannerOptions options)
{
    public IReadOnlyList<RecipeAlternative> Rank(IEnumerable<Recipe> candidates, PantrySnapshot pantry, int month, int limit)
    {
        var scorer = new WeekScorer(catalog, options, pantry, month);
        return candidates
            .Select(recipe => Describe(recipe, pantry, scorer))
            .OrderByDescending(alternative => alternative.Score)
            .ThenBy(alternative => alternative.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(limit, 0))
            .ToList();
    }

    private RecipeAlternative Describe(Recipe recipe, PantrySnapshot pantry, WeekScorer scorer)
    {
        var onHand = new List<string>();
        var expiringSoon = new List<string>();
        var toBuy = new List<string>();

        foreach (var ingredient in recipe.Ingredients.DistinctBy(i => i.MatchName()))
        {
            if (catalog.Get(ingredient.CatalogName)?.PantryStaple == true)
            {
                continue;
            }

            var name = ingredient.MatchName();
            if (!pantry.Has(name))
            {
                toBuy.Add(ingredient.DisplayName);
                continue;
            }

            onHand.Add(ingredient.DisplayName);
            if (pantry.ExpiresSoon(name))
            {
                expiringSoon.Add(ingredient.DisplayName);
            }
        }

        var score = scorer.ScoreAlone(recipe) + options.Weights.MissingIngredient * toBuy.Count;
        return new RecipeAlternative(recipe.Id, recipe.Name, recipe.IsSlowCooker, Math.Round(score, 2), onHand, expiringSoon, toBuy);
    }
}
