using System.Text.Json;
using System.Text.RegularExpressions;
using ChefBot.Core.Common;

namespace ChefBot.Core.Ingredients;

/// <summary>
/// Recognizes ingredients in messy recipe text and maps them to one shared name,
/// so recipes can be compared with each other, with the seasons and with the pantry.
/// </summary>
public sealed class IngredientCatalog
{
    /// <summary>Words that mean the text is not the fresh item, as in "canned tomatoes" or "pumpkin seeds".</summary>
    private static readonly HashSet<string> ProcessedWords = new(StringComparer.Ordinal)
    {
        "canned", "can", "cans", "jar", "jarred", "paste", "sauce", "powder", "powdered", "dried", "ground",
        "frozen", "broth", "stock", "juice", "soup", "salsa", "puree", "pickled", "pickle", "pickles",
        "flakes", "syrup", "chips", "spice", "seed", "seeds",
    };

    /// <summary>Words that turn a protein into a liquid, as in "chicken broth".</summary>
    private static readonly HashSet<string> BrothWords = new(StringComparer.Ordinal) { "broth", "stock", "bouillon", "soup" };

    private readonly Dictionary<string, CatalogEntry> _byName = new(StringComparer.Ordinal);
    private readonly List<(Regex Pattern, string Term, CatalogEntry Entry)> _matchers;

    public IngredientCatalog(IEnumerable<CatalogEntry> entries)
    {
        var terms = new Dictionary<string, CatalogEntry>(StringComparer.Ordinal);
        var normalized = entries
            .Select(entry => entry with { Name = TextNormalizer.Normalize(entry.Name) })
            .Where(entry => entry.Name.Length > 0)
            .ToList();

        // Names win over aliases, so an alias can never hide another entry's own name.
        foreach (var entry in normalized)
        {
            _byName[entry.Name] = entry;
            terms[entry.Name] = entry;
        }

        foreach (var entry in normalized)
        {
            foreach (var alias in entry.Aliases.Select(TextNormalizer.Normalize).Where(alias => alias.Length > 0))
            {
                terms.TryAdd(alias, entry);
            }
        }

        // Longest term first, so "green onion" is tried before "onion".
        _matchers = terms
            .OrderByDescending(term => term.Key.Length)
            .ThenBy(term => term.Key, StringComparer.Ordinal)
            .Select(term => (BuildPattern(term.Key), term.Key, term.Value))
            .ToList();
    }

    public IReadOnlyCollection<CatalogEntry> Entries => _byName.Values;

    /// <summary>Loads a catalog from JSON shaped like <c>{ "ingredients": [ { "name": "kale", ... } ] }</c>.</summary>
    public static IngredientCatalog FromJson(string json)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        try
        {
            var file = JsonSerializer.Deserialize<CatalogFile>(json, options);
            return new IngredientCatalog(file?.Ingredients ?? []);
        }
        catch (JsonException exception)
        {
            throw new RuleViolationException($"The ingredient catalog has a mistake in it: {exception.Message}");
        }
    }

    /// <summary>The entry with this exact catalog name, if any.</summary>
    public CatalogEntry? Get(string? catalogName) =>
        catalogName is not null && _byName.TryGetValue(catalogName, out var entry) ? entry : null;

    /// <summary>The catalog entry mentioned in free text such as "2 scallions, thinly sliced", if any.</summary>
    public CatalogEntry? Find(string? text)
    {
        var normalized = TextNormalizer.Normalize(text);
        if (normalized.Length == 0)
        {
            return null;
        }

        var words = normalized.Split(' ').ToHashSet(StringComparer.Ordinal);
        foreach (var (pattern, term, entry) in _matchers)
        {
            if (!pattern.IsMatch(normalized))
            {
                continue;
            }

            if (entry.Group == IngredientGroup.Protein && words.Overlaps(BrothWords))
            {
                continue;
            }

            if (entry.IsFreshProduce && !entry.PantryStaple && MentionsProcessing(words, term))
            {
                continue;
            }

            return entry;
        }

        return null;
    }

    /// <summary>Works out how an ingredient should be keyed, matched and displayed.</summary>
    public IngredientIdentity Identify(string text)
    {
        var display = text.Trim();
        var entry = Find(display);
        if (entry is null)
        {
            return new IngredientIdentity(TextNormalizer.Normalize(display), null, display);
        }

        // Proteins and flagged entries keep their exact wording: cod is not tilapia at the store.
        return entry.Group == IngredientGroup.Protein || entry.KeepSpecificName
            ? new IngredientIdentity(TextNormalizer.Normalize(display), entry.Name, display)
            : new IngredientIdentity(entry.Name, entry.Name, entry.Name);
    }

    private static bool MentionsProcessing(HashSet<string> words, string term)
    {
        // "lemon juice" is a deliberate alias, so words inside the matched term don't count.
        var termWords = term.Split(' ');
        return words.Any(word => ProcessedWords.Contains(word) && !termWords.Contains(word));
    }

    private static Regex BuildPattern(string term)
    {
        // Accept simple plurals: berry/berries, leaf/leaves, tomato/tomatoes, carrot/carrots.
        var stem = term.EndsWith('y') ? Regex.Escape(term[..^1]) + "(?:y|ie)"
            : term.EndsWith('f') ? Regex.Escape(term[..^1]) + "(?:f|ve)"
            : Regex.Escape(term);

        return new Regex($@"\b{stem}(?:e?s)?\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }

    private sealed record CatalogFile
    {
        public List<CatalogEntry> Ingredients { get; init; } = [];
    }
}
