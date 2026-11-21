using System.Globalization;
using System.Text.RegularExpressions;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Reads a recipe's steps for cooking mode: the times each gives ("simmer 20 minutes"), what to start ahead of time
/// (preheating the oven, boiling water, marinating overnight) and which of the recipe's ingredients each step names.
/// </summary>
internal static partial class CookingSteps
{
    /// <summary>A wait shorter than this is just part of the step, not something to plan for.</summary>
    private const int LongWaitSeconds = 30 * 60;

    /// <summary>Words in ingredient names that describe the food rather than name it, so a step isn't matched on them.</summary>
    private static readonly HashSet<string> Describing = new(StringComparer.Ordinal)
    {
        "fresh", "dried", "ground", "whole", "large", "small", "medium", "extra", "virgin", "boneless", "skinless",
        "unsalted", "salted", "chopped", "minced", "sliced", "diced", "frozen", "canned", "cooked", "baby", "plain",
        "light", "dark", "low", "sodium", "fat", "free", "raw", "white", "black", "green", "yellow", "brown", "kosher",
        "fine", "coarse", "thick", "thin", "cut", "leaves", "juice", "sauce", "powder", "pieces", "optional",
    };

    /// <summary>Each step read, in order, given the names of the recipe's ingredients in their list order.</summary>
    public static IReadOnlyList<RecipeStepResponse> Read(IReadOnlyList<string> steps, IReadOnlyList<string> ingredientNames)
    {
        var matchers = Matchers(ingredientNames);
        return steps.Select(step => new RecipeStepResponse(step, Timers(step), HeadStart(step), Mentioned(step, matchers))).ToList();
    }

    /// <summary>The times a step gives, such as "20 to 25 minutes" or "1 hour 15 minutes", in the order written.</summary>
    public static IReadOnlyList<RecipeTimerResponse> Timers(string step)
    {
        var timers = new List<RecipeTimerResponse>();
        Match? start = null;
        var seconds = 0;
        var end = 0;
        var lastUnit = int.MaxValue;
        foreach (Match part in Time().Matches(step))
        {
            var unit = UnitSeconds(part.Groups["unit"].Value);
            var amount = Amount(part.Groups["low"].Value);
            if (amount is null) continue;

            // "1 hour 30 minutes" and "1 hour and 30 minutes" are one time: a smaller unit straight after a larger one.
            var joined = start is not null && unit < lastUnit && JoinsTimes().IsMatch(step[end..part.Index]);
            if (!joined && start is not null) timers.Add(Timer(step, start.Index, end, seconds));
            if (!joined)
            {
                start = part;
                seconds = 0;
            }

            seconds += (int)Math.Round(amount.Value * unit);
            end = part.Index + part.Length;
            lastUnit = unit;
        }

        if (start is not null) timers.Add(Timer(step, start.Index, end, seconds));
        return timers.Where(timer => timer.Seconds > 0).ToList();
    }

    /// <summary>What the step needs started early, from the first sentence that asks for it, or null.</summary>
    public static RecipeHeadStartResponse? HeadStart(string step)
    {
        var sentences = SentenceBreak().Split(step.Trim()).Select(sentence => sentence.Trim().TrimEnd('.', ';', ':')).Where(sentence => sentence.Length > 0).ToList();

        // A preheat or a pot of water anywhere in the step comes first: it is the thing to start before the rest.
        var preheat = sentences.FirstOrDefault(sentence => Preheat().IsMatch(sentence));
        if (preheat is not null) return new(HeadStartKind.Preheat, preheat);
        var boil = sentences.FirstOrDefault(sentence => BoilWater().IsMatch(sentence));
        if (boil is not null) return new(HeadStartKind.Boil, boil);
        var wait = sentences.FirstOrDefault(sentence => LongWait().IsMatch(sentence)
            && (Overnight().IsMatch(sentence) || Timers(sentence).Any(timer => timer.Seconds >= LongWaitSeconds)));
        return wait is null ? null : new(HeadStartKind.Wait, wait);
    }

    private static RecipeTimerResponse Timer(string step, int start, int end, int seconds) => new(step[start..end].Trim(), seconds);

    private static int UnitSeconds(string unit) => char.ToLowerInvariant(unit[0]) switch
    {
        'h' => 3600,
        'm' => 60,
        _ => 1,
    };

    /// <summary>"20", "1.5", "1 1/2", "1½", "½", "an" or "a" as a number.</summary>
    private static double? Amount(string text)
    {
        var value = text.Trim().ToLowerInvariant();
        if (value is "a" or "an" or "one") return 1;
        double total = 0;
        foreach (var piece in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var rest = piece;
            var vulgar = rest.Length > 0 ? Vulgar(rest[^1]) : null;
            if (vulgar is not null)
            {
                total += vulgar.Value;
                rest = rest[..^1];
                if (rest.Length == 0) continue;
            }

            var slash = rest.IndexOf('/', StringComparison.Ordinal);
            if (slash > 0
                && double.TryParse(rest[..slash], NumberStyles.Float, CultureInfo.InvariantCulture, out var top)
                && double.TryParse(rest[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var bottom)
                && bottom > 0)
            {
                total += top / bottom;
            }
            else if (double.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                total += number;
            }
            else
            {
                return null;
            }
        }

        return total;
    }

    private static double? Vulgar(char character) => character switch
    {
        '½' => .5,
        '¼' => .25,
        '¾' => .75,
        '⅓' => 1d / 3,
        '⅔' => 2d / 3,
        _ => null,
    };

    /// <summary>How each ingredient is recognised in a step: its whole name, or a word of it that no other ingredient shares.</summary>
    private static List<(int Index, string Name, List<string> Words)> Matchers(IReadOnlyList<string> names)
    {
        var normalized = names.Select(TextNormalizer.Normalize).ToList();
        var wordCounts = normalized
            .SelectMany(name => name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct())
            .GroupBy(word => word)
            .ToDictionary(group => group.Key, group => group.Count());
        return normalized
            .Select((name, index) => (index, name, name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length >= 4 && !Describing.Contains(word) && wordCounts[word] == 1 && !word.All(char.IsDigit))
                .ToList()))
            .Where(matcher => matcher.name.Length > 0)
            .ToList();
    }

    private static List<int> Mentioned(string step, List<(int Index, string Name, List<string> Words)> matchers)
    {
        var words = $" {TextNormalizer.Normalize(step)} ";
        return matchers
            .Where(matcher => Says(words, matcher.Name) || matcher.Words.Any(word => Says(words, word)))
            .Select(matcher => matcher.Index)
            .ToList();
    }

    /// <summary>True when the padded, normalized step holds the words, singular or plural.</summary>
    private static bool Says(string paddedStep, string words)
    {
        string[] forms = words.EndsWith("es", StringComparison.Ordinal) ? [words, words[..^2], words[..^1]]
            : words.EndsWith('s') ? [words, words[..^1]]
            : [words, words + "s", words + "es"];
        return forms.Any(form => form.Length > 0 && paddedStep.Contains($" {form} ", StringComparison.Ordinal));
    }

    [GeneratedRegex(@"(?<low>\d+(?:\.\d+)?(?:\s+\d+/\d+|[½¼¾⅓⅔])?|\d+/\d+|[½¼¾⅓⅔]|\ban?\b|\bone\b)(?:\s*(?:-|–|—|to|or)\s*(?<high>\d+(?:\.\d+)?))?\s*-?\s*(?<unit>hours?|hrs?|minutes?|mins?|seconds?|secs?)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Time();

    [GeneratedRegex(@"^\s*(?:,|and|,\s*and|plus)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JoinsTimes();

    [GeneratedRegex(@"(?<=[.!?;])\s+")]
    private static partial Regex SentenceBreak();

    [GeneratedRegex(@"\bpre-?heat\b.*\b(?:oven|broiler|grill|air\s*fryer|smoker|\d+\s*°|\d+\s*degrees)|\bheat\s+(?:the\s+|your\s+)?(?:oven|broiler|grill|air\s*fryer)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Preheat();

    [GeneratedRegex(@"\bbring\b.*\bwater\b.*\bto\s+(?:a\s+)?(?:rolling\s+|full\s+)?boil\b|\bboil\s+(?:a\s+)?(?:large\s+)?(?:pot|kettle)\s+of\s+(?:salted\s+)?water\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BoilWater();

    [GeneratedRegex(@"\b(?:marinat\w*|refrigerat\w*|chill\w*|soak\w*|rise|rises|rising|proof\w*|rest\w*|freez\w*|thaw\w*|brin\w*|cure|curing|set\s+aside|let\s+(?:it\s+|them\s+)?(?:sit|stand))\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LongWait();

    [GeneratedRegex(@"\bovernight\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Overnight();
}
