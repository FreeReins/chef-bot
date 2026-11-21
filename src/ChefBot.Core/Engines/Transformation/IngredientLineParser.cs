using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Reads free-text ingredient lines such as "1 1/2 cups brown rice", "2 ½ cups half and half", "2-4 chicken
/// breasts" or "salt". The name drops prep notes after a comma and anything in brackets, so
/// "chicken breasts, (boneless, skinless)" is named "chicken breasts".
/// </summary>
internal static class IngredientLineParser
{
    private const string Number = @"\d+\s+\d+/\d+|\d+/\d+|\d+(?:\.\d+)?";

    /// <summary>An amount or a range of amounts ("2-4", "2 – 4", "2 to 4"), then an optional size in brackets.</summary>
    private static readonly Regex LeadingAmount = new(
        $@"^(?<amount>{Number})(?:\s*(?:-|–|—|to)\s*(?<upper>{Number}))?\s*(?:\([^)]*\)\s*)?(?<rest>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Checkboxes and bullets some recipe sites put in front of each line.</summary>
    private static readonly Regex LeadingMarker = new(
        @"^(?:[▢☐□■•●◦▪*]|-(?!\d))\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Brackets = new(@"\([^)]*\)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Dictionary<char, string> VulgarFractions = new()
    {
        ['½'] = "1/2", ['⅓'] = "1/3", ['⅔'] = "2/3", ['¼'] = "1/4", ['¾'] = "3/4",
        ['⅕'] = "1/5", ['⅖'] = "2/5", ['⅗'] = "3/5", ['⅘'] = "4/5", ['⅙'] = "1/6", ['⅚'] = "5/6",
        ['⅛'] = "1/8", ['⅜'] = "3/8", ['⅝'] = "5/8", ['⅞'] = "7/8",
    };

    public static ParsedIngredient Parse(string line)
    {
        var text = Clean(line);
        var match = LeadingAmount.Match(text);
        if (!match.Success)
        {
            return new ParsedIngredient(null, string.Empty, Name(text));
        }

        var rest = match.Groups["rest"].Value.Trim();
        var quantity = ParseAmount(match.Groups["amount"].Value);
        if (match.Groups["upper"].Success && ParseAmount(match.Groups["upper"].Value) is decimal upper && upper > quantity)
        {
            quantity = upper;
        }

        if (rest.Length == 0 || quantity is null)
        {
            return new ParsedIngredient(null, string.Empty, Name(text));
        }

        var words = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var count in new[] { 2, 1 })
        {
            if (words.Length > count && Units.TryParse(string.Join(' ', words[..count]), out var unit))
            {
                var name = string.Join(' ', words[count..]);
                name = name.StartsWith("of ", StringComparison.OrdinalIgnoreCase) ? name[3..].Trim() : name;
                return new ParsedIngredient(quantity, unit, Name(name));
            }
        }

        return new ParsedIngredient(quantity, string.Empty, Name(rest));
    }

    /// <summary>Removes leading bullets and writes fractions such as "1½" and "1⁄2" as "1 1/2" and "1/2".</summary>
    private static string Clean(string line)
    {
        var builder = new StringBuilder(line.Length + 4);
        foreach (var character in line)
        {
            if (VulgarFractions.TryGetValue(character, out var fraction))
            {
                if (builder.Length > 0 && char.IsDigit(builder[^1]))
                {
                    builder.Append(' ');
                }

                builder.Append(fraction);
            }
            else
            {
                builder.Append(character == '⁄' ? '/' : character);
            }
        }

        var text = builder.ToString().Trim();
        text = Regex.Replace(text, @"(?<=\d)\s*/\s*(?=\d)", "/");
        return LeadingMarker.Replace(text, string.Empty).Trim();
    }

    /// <summary>The ingredient's name without prep notes: "chicken breasts , (boneless, skinless)" is "chicken breasts".</summary>
    private static string Name(string text)
    {
        var name = Brackets.Replace(text, " ");
        var comma = name.IndexOf(',', StringComparison.Ordinal);
        if (comma >= 0)
        {
            name = name[..comma];
        }

        name = Spaces.Replace(name, " ").Trim();
        return name.Length > 0 ? name : text.Trim();
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
