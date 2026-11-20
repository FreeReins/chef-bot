using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>A recipe that could replace a planned meal, and what choosing it would mean.</summary>
internal sealed record Alternative(
    Recipe Recipe,
    double Score,
    IReadOnlyList<string> OnHand,
    IReadOnlyList<string> ExpiringSoon,
    IReadOnlyList<string> ToBuy);

/// <summary>One part of a creative meal.</summary>
/// <param name="Name">The ingredient.</param>
/// <param name="Group">Its role: whole grain, protein, vegetable or fruit.</param>
/// <param name="OnHand">False when it would have to be bought.</param>
/// <param name="ExpiresOn">When the pantry's stock of it expires, if known.</param>
internal sealed record IdeaPart(string Name, IngredientGroup Group, bool OnHand, DateOnly? ExpiresOn);

/// <summary>A meal idea built from a whole grain, a protein and a vegetable or fruit.</summary>
internal sealed record MealIdea(string Title, IReadOnlyList<IdeaPart> Parts);

/// <summary>Everything that could replace one meal.</summary>
internal sealed record Suggestions(IReadOnlyList<Alternative> Recipes, IReadOnlyList<MealIdea> Ideas);

/// <summary>
/// Suggests swaps for a planned meal: recipes ranked by how well they use the pantry, especially food that is about
/// to expire, and simple grain, protein and produce ideas. Reads and saves nothing.
/// </summary>
internal sealed class SuggestionEngine(IngredientCatalog catalog, PlannerOptions options)
{
    private const string Protein = "{protein}";
    private const string Produce = "{produce}";
    private const string Grain = "{grain}";

    private static readonly string[] Templates =
    [
        $"{Protein} and {Produce} {Grain} bowl",
        $"{Produce} and {Protein} stir-fry over {Grain}",
        $"Sheet-pan {Protein} with {Produce} and {Grain}",
        $"{Grain} salad with {Protein} and {Produce}",
        $"{Protein} and {Produce} skillet with {Grain}",
    ];

    /// <summary>
    /// Replacements for <paramref name="meal"/>: recipes of the same course that aren't already on the plan, and,
    /// for lunch and dinner, creative ideas.
    /// </summary>
    public Suggestions ForMeal(
        MealPlan plan, PlannedMeal meal, IEnumerable<Recipe> library, PantrySnapshot pantry, int recipeLimit, int ideaCount, Random random)
    {
        var course = CourseFor(meal.Slot);
        var alreadyPlanned = plan.Meals.Where(m => m.RecipeId is not null).Select(m => m.RecipeId!.Value).ToHashSet();
        var candidates = library.Where(recipe => recipe.Course == course && !alreadyPlanned.Contains(recipe.Id));

        // Grain, protein and produce plates make sense for lunch and dinner only.
        var ideas = meal.Slot is MealSlot.Lunch or MealSlot.Dinner ? Ideas(pantry, ideaCount, random) : [];
        return new Suggestions(Rank(candidates, pantry, plan.WeekStart.Month, recipeLimit), ideas);
    }

    /// <summary>Recipes ranked by how well they use the pantry, best first.</summary>
    public IReadOnlyList<Alternative> Rank(IEnumerable<Recipe> candidates, PantrySnapshot pantry, int month, int limit)
    {
        var scorer = new WeekScorer(catalog, options, pantry, month);
        return candidates
            .Select(recipe => Describe(recipe, pantry, scorer))
            .OrderByDescending(alternative => alternative.Score)
            .ThenBy(alternative => alternative.Recipe.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(limit, 0))
            .ToList();
    }

    /// <summary>
    /// Up to <paramref name="count"/> different meal ideas from what is in the pantry, following the usual balanced
    /// plate. Food that expires soonest is the most likely to be picked. Fewer come back when the pantry offers few
    /// combinations.
    /// </summary>
    public IReadOnlyList<MealIdea> Ideas(PantrySnapshot pantry, int count, Random random)
    {
        var proteins = Candidates(pantry, IngredientGroup.Protein);
        var produce = Candidates(pantry, IngredientGroup.Vegetable, IngredientGroup.Fruit);
        var grains = Candidates(pantry, IngredientGroup.WholeGrain);
        if (proteins.Count == 0 || produce.Count == 0 || grains.Count == 0)
        {
            return [];
        }

        var meals = new List<MealIdea>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var attempt = 0; meals.Count < count && attempt < count * 10; attempt++)
        {
            var protein = Pick(proteins, pantry, random);
            var vegetable = Pick(produce, pantry, random);
            var grain = Pick(grains, pantry, random);
            if (!seen.Add($"{protein.Name}|{vegetable.Name}|{grain.Name}"))
            {
                continue;
            }

            var title = Templates[random.Next(Templates.Length)]
                .Replace(Protein, protein.Name)
                .Replace(Produce, vegetable.Name)
                .Replace(Grain, grain.Name);

            meals.Add(new MealIdea(char.ToUpperInvariant(title[0]) + title[1..], [protein, vegetable, grain]));
        }

        return meals;
    }

    /// <summary>The kind of recipe that can fill a meal slot.</summary>
    public static RecipeCourse CourseFor(MealSlot slot) => slot switch
    {
        MealSlot.Breakfast => RecipeCourse.Breakfast,
        MealSlot.Snack => RecipeCourse.Snack,
        MealSlot.Sweet => RecipeCourse.Sweet,
        _ => RecipeCourse.Main,
    };

    private Alternative Describe(Recipe recipe, PantrySnapshot pantry, WeekScorer scorer)
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
        return new Alternative(recipe, Math.Round(score, 2), onHand, expiringSoon, toBuy);
    }

    /// <summary>What is on hand for a role. When nothing is, every catalog option, marked as something to buy.</summary>
    private List<IdeaPart> Candidates(PantrySnapshot pantry, params IngredientGroup[] groups)
    {
        var entries = catalog.Entries
            .Where(entry => groups.Contains(entry.Group) && !entry.PantryStaple)
            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
            .ToList();

        var onHand = entries
            .Where(entry => pantry.Has(entry.Name))
            .Select(entry => new IdeaPart(entry.Name, entry.Group, true, pantry.ExpiryOf(entry.Name)))
            .ToList();

        return onHand.Count > 0
            ? onHand
            : entries.Select(entry => new IdeaPart(entry.Name, entry.Group, false, null)).ToList();
    }

    private static IdeaPart Pick(List<IdeaPart> candidates, PantrySnapshot pantry, Random random)
    {
        var weights = candidates.Select(candidate => Weight(pantry.DaysLeft(candidate.Name))).ToList();
        var roll = random.NextDouble() * weights.Sum();
        for (var index = 0; index < candidates.Count; index++)
        {
            roll -= weights[index];
            if (roll <= 0)
            {
                return candidates[index];
            }
        }

        return candidates[^1];
    }

    /// <summary>Food expiring within three days is six times as likely to be picked; within a week, three times.</summary>
    private static double Weight(int? daysLeft) => daysLeft switch
    {
        null => 1,
        <= 3 => 6,
        <= 7 => 3,
        _ => 1,
    };
}
