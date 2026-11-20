using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChefBot.Core.Managers.Contracts;

namespace ChefBot.Core.Engines.Transformation;

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

    /// <summary>A cooking time in minutes: a number of minutes, or text that <see cref="DurationParser"/> reads.</summary>
    public static int? Minutes(JsonElement? value) => value switch
    {
        { ValueKind: JsonValueKind.Number } number when number.TryGetDouble(out var minutes) => DurationParser.Minutes(minutes.ToString(CultureInfo.InvariantCulture)),
        { ValueKind: JsonValueKind.String } text => DurationParser.Minutes(text.GetString()),
        _ => null,
    };

    /// <summary>
    /// Lines of text from a string (one per line), an array, or objects carrying <c>text</c> or <c>name</c>. Nested
    /// lists, such as schema.org's HowToSection <c>itemListElement</c>, are flattened. Blank lines are dropped.
    /// </summary>
    public static List<string> TextList(JsonElement? value) => Lines(value).Select(line => line.Trim()).Where(line => line.Length > 0).ToList();

    /// <summary>Image addresses from a string, an array, or ImageObjects with a <c>url</c> or <c>contentUrl</c>.</summary>
    public static List<string> ImageUrls(JsonElement? value) => value switch
    {
        { ValueKind: JsonValueKind.String } text => [.. new[] { text.GetString()?.Trim() }.OfType<string>().Where(url => url.Length > 0)],
        { ValueKind: JsonValueKind.Array } list => list.EnumerateArray().SelectMany(item => ImageUrls(item)).Distinct(StringComparer.Ordinal).ToList(),
        { ValueKind: JsonValueKind.Object } image => ImageUrls(Property(image, "url") ?? Property(image, "contentUrl")),
        _ => [],
    };

    /// <summary>A name from a string, an object with a <c>name</c>, or a list of them joined with commas.</summary>
    public static string? NameList(JsonElement? value)
    {
        IEnumerable<string> Each(JsonElement? item) => item switch
        {
            { ValueKind: JsonValueKind.String } text => [text.GetString() ?? string.Empty],
            { ValueKind: JsonValueKind.Array } list => list.EnumerateArray().SelectMany(entry => Each(entry)),
            { ValueKind: JsonValueKind.Object } named => Each(Property(named, "name")),
            _ => [],
        };

        var names = Each(value).Select(name => name.Trim()).Where(name => name.Length > 0).ToList();
        return names.Count > 0 ? string.Join(", ", names) : null;
    }

    /// <summary>
    /// A yield's wording when it says more than the servings do: "one 9-inch pie" or "24 cookies", but not "4" or
    /// "4 servings".
    /// </summary>
    public static string? YieldWords(string? text) =>
        Tidy(text) is { } tidy && !OnlyServings().IsMatch(tidy) ? tidy : null;

    /// <summary>Text that isn't blank, trimmed, or null.</summary>
    public static string? Tidy(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static IEnumerable<string> Lines(JsonElement? value) => value switch
    {
        { ValueKind: JsonValueKind.String } text => (text.GetString() ?? string.Empty).Split('\n'),
        { ValueKind: JsonValueKind.Array } list => list.EnumerateArray().SelectMany(item => Lines(item)),
        { ValueKind: JsonValueKind.Object } item when Property(item, "itemListElement") is { } inner => Lines(inner),
        { ValueKind: JsonValueKind.Object } item => Lines(Property(item, "text") ?? Property(item, "name")),
        _ => [],
    };

    [GeneratedRegex(@"^\d+(\.\d+)?\s*(servings?|portions?|people|persons?|serves)?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OnlyServings();

    [GeneratedRegex(@"\d+(\.\d+)?")]
    private static partial Regex FirstNumber();
}
