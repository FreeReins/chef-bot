using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChefBot.Core.Utilities;

/// <summary>Puts free text into one comparable form.</summary>
internal static class TextNormalizer
{
    private static readonly Regex NonAlphanumeric = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Lower-cases, strips accents and punctuation, and collapses whitespace,
    /// so "Jalapeño, diced" and "jalapeno diced" compare equal.
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return NonAlphanumeric.Replace(builder.ToString(), " ").Trim();
    }
}

/// <summary>Gives units one spelling so "cups" and "Cup" add up.</summary>
internal static class Units
{
    private static readonly Dictionary<string, string> Canonical = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cup"] = "cup", ["cups"] = "cup", ["c"] = "cup",
        ["tablespoon"] = "tbsp", ["tablespoons"] = "tbsp", ["tbsp"] = "tbsp", ["tbs"] = "tbsp",
        ["teaspoon"] = "tsp", ["teaspoons"] = "tsp", ["tsp"] = "tsp",
        ["pound"] = "lb", ["pounds"] = "lb", ["lb"] = "lb", ["lbs"] = "lb",
        ["ounce"] = "oz", ["ounces"] = "oz", ["oz"] = "oz",
        ["gram"] = "g", ["grams"] = "g", ["g"] = "g",
        ["kilogram"] = "kg", ["kilograms"] = "kg", ["kg"] = "kg",
        ["milliliter"] = "ml", ["milliliters"] = "ml", ["ml"] = "ml",
        ["liter"] = "l", ["liters"] = "l", ["l"] = "l",
        ["quart"] = "quart", ["quarts"] = "quart", ["pint"] = "pint", ["pints"] = "pint",
        ["can"] = "can", ["cans"] = "can", ["jar"] = "jar", ["jars"] = "jar",
        ["package"] = "package", ["packages"] = "package", ["pkg"] = "package",
        ["bunch"] = "bunch", ["bunches"] = "bunch", ["head"] = "head", ["heads"] = "head",
        ["clove"] = "clove", ["cloves"] = "clove", ["stalk"] = "stalk", ["stalks"] = "stalk",
        ["sprig"] = "sprig", ["sprigs"] = "sprig", ["slice"] = "slice", ["slices"] = "slice",
        ["piece"] = "piece", ["pieces"] = "piece", ["pinch"] = "pinch",
    };

    /// <summary>True when <paramref name="word"/> is a unit ChefBot recognizes.</summary>
    public static bool TryParse(string word, out string unit)
    {
        if (Canonical.TryGetValue(word.Trim().TrimEnd('.'), out var found))
        {
            unit = found;
            return true;
        }

        unit = string.Empty;
        return false;
    }

    /// <summary>The canonical spelling, or the lower-cased text for units ChefBot doesn't know.</summary>
    public static string Normalize(string? unit)
    {
        var text = unit?.Trim() ?? string.Empty;
        return Canonical.TryGetValue(text, out var found) ? found : text.ToLowerInvariant();
    }

    public static bool Same(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}

internal static class Usernames
{
    /// <summary>Sign-in names are compared trimmed and lower-case.</summary>
    public static string Normalize(string? username) => (username ?? string.Empty).Trim().ToLowerInvariant();
}
