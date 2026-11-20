using System.Globalization;
using System.Text.RegularExpressions;

namespace ChefBot.Core.Ingredients;

/// <summary>An ingredient line split into its parts. Quantity is null when the line has no leading amount.</summary>
public sealed record ParsedIngredient(decimal? Quantity, string Unit, string Name);

/// <summary>Reads free-text ingredient lines such as "1 1/2 cups brown rice" or "salt".</summary>
public static class IngredientLineParser
{
    private static readonly Regex LeadingAmount = new(
        @"^(?<amount>\d+\s+\d+/\d+|\d+/\d+|\d+(?:\.\d+)?)\s*(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static ParsedIngredient Parse(string line)
    {
        var text = line.Trim();
        var match = LeadingAmount.Match(text);
        if (!match.Success)
        {
            return new ParsedIngredient(null, string.Empty, text);
        }

        var rest = match.Groups["rest"].Value.Trim();
        var quantity = ParseAmount(match.Groups["amount"].Value);
        if (rest.Length == 0 || quantity is null)
        {
            return new ParsedIngredient(null, string.Empty, text);
        }

        var words = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 2 && Units.TryParse(words[0], out var unit))
        {
            var name = words[1].StartsWith("of ", StringComparison.OrdinalIgnoreCase) ? words[1][3..].Trim() : words[1];
            return new ParsedIngredient(quantity, unit, name);
        }

        return new ParsedIngredient(quantity, string.Empty, rest);
    }

    /// <summary>Reads "2", "0.5", "1/2" or "1 1/2". Returns null for amounts that make no sense.</summary>
    private static decimal? ParseAmount(string amount)
    {
        decimal total = 0;
        foreach (var part in amount.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var fraction = part.Split('/');
            if (!decimal.TryParse(fraction[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                return null;
            }

            if (fraction.Length == 2)
            {
                if (!decimal.TryParse(fraction[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var divisor) || divisor == 0)
                {
                    return null;
                }

                value /= divisor;
            }

            total += value;
        }

        return total > 0 ? total : null;
    }
}
