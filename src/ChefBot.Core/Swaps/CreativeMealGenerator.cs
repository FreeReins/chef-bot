using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;

namespace ChefBot.Core.Swaps;

/// <summary>One part of a creative meal.</summary>
/// <param name="Name">The ingredient.</param>
/// <param name="Group">Its role: whole grain, protein, vegetable or fruit.</param>
/// <param name="OnHand">False when it would have to be bought.</param>
/// <param name="ExpiresOn">When the pantry's stock of it expires, if known.</param>
public sealed record CreativeComponent(string Name, IngredientGroup Group, bool OnHand, DateOnly? ExpiresOn);

/// <summary>A meal idea built from a whole grain, a protein and a vegetable or fruit.</summary>
public sealed record CreativeMeal(string Title, IReadOnlyList<CreativeComponent> Components)
{
    /// <summary>The ingredient names, ready to send back when choosing this idea.</summary>
    public IReadOnlyList<string> Ingredients => Components.Select(component => component.Name).ToList();
}

/// <summary>
/// Invents simple meals from what is in the pantry, following the usual balanced plate:
/// a whole grain, a protein and produce. Food that expires soonest is the most likely to be picked.
/// </summary>
public sealed class CreativeMealGenerator(IngredientCatalog catalog)
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

    /// <summary>Up to <paramref name="count"/> different ideas. Fewer come back when the pantry offers few combinations.</summary>
    public IReadOnlyList<CreativeMeal> Generate(PantrySnapshot pantry, int count, Random random)
    {
        var proteins = Candidates(pantry, IngredientGroup.Protein);
        var produce = Candidates(pantry, IngredientGroup.Vegetable, IngredientGroup.Fruit);
        var grains = Candidates(pantry, IngredientGroup.WholeGrain);
        if (proteins.Count == 0 || produce.Count == 0 || grains.Count == 0)
        {
            return [];
        }

        var meals = new List<CreativeMeal>();
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

            meals.Add(new CreativeMeal(char.ToUpperInvariant(title[0]) + title[1..], [protein, vegetable, grain]));
        }

        return meals;
    }

    /// <summary>What is on hand for a role. When nothing is, every catalog option, marked as something to buy.</summary>
    private List<CreativeComponent> Candidates(PantrySnapshot pantry, params IngredientGroup[] groups)
    {
        var entries = catalog.Entries
            .Where(entry => groups.Contains(entry.Group) && !entry.PantryStaple)
            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
            .ToList();

        var onHand = entries
            .Where(entry => pantry.Has(entry.Name))
            .Select(entry => new CreativeComponent(entry.Name, entry.Group, true, pantry.ExpiryOf(entry.Name)))
            .ToList();

        return onHand.Count > 0
            ? onHand
            : entries.Select(entry => new CreativeComponent(entry.Name, entry.Group, false, null)).ToList();
    }

    private static CreativeComponent Pick(List<CreativeComponent> candidates, PantrySnapshot pantry, Random random)
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
