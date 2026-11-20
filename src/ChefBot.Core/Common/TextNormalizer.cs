using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ChefBot.Core.Common;

public static class TextNormalizer
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
