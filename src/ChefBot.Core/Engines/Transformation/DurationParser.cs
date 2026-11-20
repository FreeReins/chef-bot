using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Reads a cooking time as whole minutes: ISO 8601 durations as recipe sites write them ("PT1H30M"), a bare number
/// of minutes ("45"), or words ("1 hour 30 minutes", "45 min", "1h 30m", "1.5 hours").
/// </summary>
internal static partial class DurationParser
{
    /// <summary>The time in minutes, rounded, or null when the text holds no time or it isn't positive.</summary>
    public static int? Minutes(string? text)
    {
        var value = text?.Trim() ?? string.Empty;
        if (value.Length == 0) return null;

        double minutes;
        if (value.StartsWith('P') || value.StartsWith('p'))
        {
            try
            {
                minutes = XmlConvert.ToTimeSpan(value.ToUpperInvariant()).TotalMinutes;
            }
            catch (FormatException)
            {
                return null;
            }
        }
        else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var bare))
        {
            minutes = bare;
        }
        else
        {
            var parts = Part().Matches(value);
            if (parts.Count == 0) return null;
            minutes = parts.Sum(part =>
            {
                var amount = double.Parse(part.Groups["amount"].Value, CultureInfo.InvariantCulture);
                var unit = part.Groups["unit"].Value.ToLowerInvariant();
                return unit.StartsWith('d') ? amount * 1440 : unit.StartsWith('h') ? amount * 60 : amount;
            });
        }

        var rounded = (int)Math.Round(minutes, MidpointRounding.AwayFromZero);
        return rounded > 0 ? rounded : null;
    }

    [GeneratedRegex(@"(?<amount>\d+(?:\.\d+)?)\s*(?<unit>days?|d|hours?|hrs?|h|minutes?|mins?|m)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Part();
}
