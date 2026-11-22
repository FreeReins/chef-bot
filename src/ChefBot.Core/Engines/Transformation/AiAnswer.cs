using System.Globalization;
using System.Text.Json;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Reads an AI model's JSON answer leniently. Models are asked for empty text, zero or an empty list when they don't
/// know something, since not every service accepts nulls in a schema; those all read as "not given" here.
/// </summary>
internal static class AiAnswer
{
    /// <summary>The answer's top-level object, or null when it isn't a JSON object.</summary>
    public static JsonElement? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>A text property, trimmed; null when missing or empty.</summary>
    public static string? Text(JsonElement answer, string name) =>
        answer.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()!.Trim() is { Length: > 0 } text
            ? text
            : null;

    /// <summary>A whole number above zero; null when missing, zero or not a number.</summary>
    public static int? Minutes(JsonElement answer, string name) =>
        Number(answer, name) is { } number && number >= 1 && number <= 100_000 ? (int)Math.Round(number) : null;

    /// <summary>A number above zero; null when missing, zero or not a number.</summary>
    public static decimal? Number(JsonElement answer, string name)
    {
        if (!answer.TryGetProperty(name, out var value))
        {
            return null;
        }

        decimal number;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var parsed))
        {
            number = parsed;
        }
        else if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var written))
        {
            number = written;
        }
        else
        {
            return null;
        }

        return number > 0 ? number : null;
    }

    /// <summary>True only for a JSON true.</summary>
    public static bool Flag(JsonElement answer, string name) =>
        answer.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>A list of texts, each trimmed, without empty ones. Empty when missing.</summary>
    public static List<string> Lines(JsonElement answer, string name) =>
        answer.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!.Trim())
                .Where(line => line.Length > 0)
                .ToList()
            : [];
}
