using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChefBot.Core.Common;

namespace ChefBot.Core.Recipes.Files;

/// <summary>Small helpers shared by the JSON-based recipe file formats.</summary>
internal static partial class JsonValues
{
    /// <exception cref="RuleViolationException">The text isn't JSON.</exception>
    public static JsonDocument Parse(string text, string kind)
    {
        try
        {
            return JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        }
        catch (JsonException)
        {
            throw new RuleViolationException($"That file isn't a {kind} file. Export your recipes as JSON and try again.");
        }
    }

    /// <summary>A property by name, ignoring case, so "Name" and "name" both work.</summary>
    public static JsonElement? Property(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        }

        return null;
    }

    public static string? Text(JsonElement? value) => value is { ValueKind: JsonValueKind.String } text ? text.GetString() : null;

    /// <summary>The objects in an array property, or nothing when it is missing.</summary>
    public static IEnumerable<JsonElement> Rows(JsonElement element, string name) =>
        Property(element, name) is { ValueKind: JsonValueKind.Array } list
            ? list.EnumerateArray().Where(row => row.ValueKind == JsonValueKind.Object)
            : [];

    /// <summary>A number, or the first number in text such as "4 servings". Null when there is none or it isn't positive.</summary>
    public static decimal? PositiveAmount(JsonElement? value)
    {
        decimal? found = value switch
        {
            { ValueKind: JsonValueKind.Number } number when number.TryGetDecimal(out var amount) => amount,
            { ValueKind: JsonValueKind.Array } list => list.EnumerateArray().Select(item => PositiveAmount(item)).FirstOrDefault(amount => amount is not null),
            { ValueKind: JsonValueKind.String } words when FirstNumber().Match(words.GetString() ?? string.Empty) is { Success: true } match =>
                decimal.Parse(match.Value, CultureInfo.InvariantCulture),
            _ => null,
        };
        return found is > 0m ? found : null;
    }

    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex FirstNumber();
}
