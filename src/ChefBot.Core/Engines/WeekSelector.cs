using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// Picks one recipe per slot so the whole set scores as high as possible.
/// It starts from several random weeks and improves each one swap at a time,
/// which is fast enough for libraries of thousands of recipes.
/// </summary>
internal static class WeekSelector
{
    /// <param name="slots">What needs filling, for example the dinner days.</param>
    /// <param name="pools">The recipes allowed in each slot.</param>
    /// <param name="alongside">Recipes already chosen, which are scored with the picks and never picked again.</param>
    /// <param name="score">Scores a complete set of recipes.</param>
    /// <param name="random">Source of the random starting weeks.</param>
    /// <param name="restarts">How many starting weeks to try.</param>
    /// <returns>A recipe for every slot, or null when the pools can't fill them with different recipes.</returns>
    public static Dictionary<TSlot, Recipe>? Choose<TSlot>(
        IReadOnlyList<TSlot> slots,
        IReadOnlyDictionary<TSlot, IReadOnlyList<Recipe>> pools,
        IReadOnlyCollection<Recipe> alongside,
        Func<IEnumerable<Recipe>, double> score,
        Random random,
        int restarts = 12)
        where TSlot : notnull
    {
        var fixedMeals = alongside.ToList();
        Dictionary<TSlot, Recipe>? best = null;
        var bestScore = double.NegativeInfinity;

        for (var attempt = 0; attempt < restarts; attempt++)
        {
            var candidate = RandomStart(slots, pools, fixedMeals, random);
            if (candidate is null)
            {
                continue;
            }

            Improve(slots, pools, fixedMeals, score, candidate);
            var total = score(candidate.Values.Concat(fixedMeals));
            if (total > bestScore)
            {
                best = candidate;
                bestScore = total;
            }
        }

        return best;
    }

    private static Dictionary<TSlot, Recipe>? RandomStart<TSlot>(
        IReadOnlyList<TSlot> slots,
        IReadOnlyDictionary<TSlot, IReadOnlyList<Recipe>> pools,
        List<Recipe> fixedMeals,
        Random random)
        where TSlot : notnull
    {
        var taken = fixedMeals.Select(recipe => recipe.Id).ToHashSet();
        var start = new Dictionary<TSlot, Recipe>();

        // Fill the most restricted slots first so a small pool isn't used up elsewhere.
        foreach (var slot in slots.OrderBy(slot => pools[slot].Count))
        {
            var available = pools[slot].Where(recipe => !taken.Contains(recipe.Id)).ToList();
            if (available.Count == 0)
            {
                return null;
            }

            var pick = available[random.Next(available.Count)];
            start[slot] = pick;
            taken.Add(pick.Id);
        }

        return start;
    }

    private static void Improve<TSlot>(
        IReadOnlyList<TSlot> slots,
        IReadOnlyDictionary<TSlot, IReadOnlyList<Recipe>> pools,
        List<Recipe> fixedMeals,
        Func<IEnumerable<Recipe>, double> score,
        Dictionary<TSlot, Recipe> current)
        where TSlot : notnull
    {
        var comparer = EqualityComparer<TSlot>.Default;
        var improved = true;
        while (improved)
        {
            improved = false;
            foreach (var slot in slots)
            {
                var others = current
                    .Where(pair => !comparer.Equals(pair.Key, slot))
                    .Select(pair => pair.Value)
                    .Concat(fixedMeals)
                    .ToList();
                var taken = others.Select(recipe => recipe.Id).ToHashSet();
                var bestHere = score(others.Append(current[slot]));

                foreach (var option in pools[slot])
                {
                    if (taken.Contains(option.Id) || option.Id == current[slot].Id)
                    {
                        continue;
                    }

                    var trial = score(others.Append(option));
                    if (trial > bestHere + 1e-9)
                    {
                        current[slot] = option;
                        bestHere = trial;
                        improved = true;
                    }
                }
            }
        }
    }
}
