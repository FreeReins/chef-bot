using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// Writes meal plans as an iCalendar feed (RFC 5545) for calendar apps to subscribe to, and makes and checks the
/// feed's secret. Each dated meal is an event at its slot's time; meals without a day (snacks, sweets, drinks) and
/// skipped meals are left out. Times are floating, with no time zone, so each calendar shows them at that time of day
/// wherever it is. Reads and saves nothing.
/// </summary>
internal sealed class CalendarEngine
{
    /// <summary>The longest line, in UTF-8 bytes, before it is folded onto the next (RFC 5545 section 3.1).</summary>
    private const int LineOctets = 75;

    /// <summary>A new secret for the feed's address.</summary>
    public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    /// <summary>True when the feed is on and <paramref name="token"/> is its secret, compared in constant time.</summary>
    public static bool Matches(CalendarFeed feed, string? token) =>
        feed.Token.Length > 0 && token is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(feed.Token), Encoding.UTF8.GetBytes(token.Trim()));

    /// <summary>The feed: every dated meal of the given plans, linking to its recipe under <paramref name="site"/>.</summary>
    /// <param name="plans">The plans to show.</param>
    /// <param name="site">ChefBot's own address, ending in a slash, for links to recipes.</param>
    /// <param name="now">When the feed is written, for each event's timestamp.</param>
    /// <param name="times">When each meal starts, how long it lasts and when slow cooker reminders go off.</param>
    public string Write(IEnumerable<MealPlan> plans, Uri site, DateTimeOffset now, CalendarSettings times)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//ChefBot//Meal plan//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "X-WR-CALNAME:ChefBot meals",
            $"X-WR-CALDESC:{Escape("The meals ChefBot has planned for the confirmed weeks.")}",
            "REFRESH-INTERVAL;VALUE=DURATION:PT1H",
            "X-PUBLISHED-TTL:PT1H",
        };

        var stamp = now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var meals = plans
            .SelectMany(plan => plan.Meals)
            .Where(meal => meal.Date is not null && meal.Status != MealStatus.Skipped)
            .OrderBy(meal => meal.Date)
            .ThenBy(meal => StartOf(meal.Slot, times));
        foreach (var meal in meals)
        {
            lines.AddRange(Event(meal, site, stamp, times));
        }

        lines.Add("END:VCALENDAR");
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            Fold(text, line);
        }

        return text.ToString();
    }

    /// <summary>
    /// Escapes text for a TEXT value: backslashes, semicolons, commas and line breaks (RFC 5545 section 3.3.11).
    /// </summary>
    public static string Escape(string text) => text
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace("\r\n", "\\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    /// <summary>
    /// Writes one content line, folded so no line is longer than 75 bytes: each further part starts with a space.
    /// A character is never split between lines. Every line ends in CRLF.
    /// </summary>
    public static void Fold(StringBuilder text, string line)
    {
        var used = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (used + size > LineOctets)
            {
                text.Append("\r\n ");
                used = 1;
            }

            text.Append(rune.ToString());
            used += size;
        }

        text.Append("\r\n");
    }

    private static IEnumerable<string> Event(PlannedMeal meal, Uri site, string stamp, CalendarSettings times)
    {
        var start = meal.Date!.Value.ToDateTime(StartOf(meal.Slot, times));
        var end = start.AddMinutes(Math.Max(times.MealMinutes, 5));
        var title = meal.IsLeftover ? $"{meal.Title} (leftovers)" : meal.Title;
        var recipe = meal.RecipeId is Guid recipeId ? new Uri(site, $"recipes/{recipeId}").AbsoluteUri : null;
        var slowCooker = meal.Recipe?.IsSlowCooker == true && !meal.IsLeftover;

        var notes = new List<string> { $"{meal.Portions} {(meal.Portions == 1 ? "portion" : "portions")}" };
        if (slowCooker) notes.Add("Made in the slow cooker.");
        if (meal.IsOptional) notes.Add("Optional: skip it if you'd rather.");
        if (meal.Status == MealStatus.Cooked) notes.Add("Cooked.");
        if (recipe is not null) notes.Add(recipe);

        yield return "BEGIN:VEVENT";
        yield return $"UID:{meal.Id}@chefbot";
        yield return $"DTSTAMP:{stamp}";
        yield return $"DTSTART:{Floating(start)}";
        yield return $"DTEND:{Floating(end)}";
        yield return $"SUMMARY:{Escape($"{meal.Slot}: {title}")}";
        yield return $"DESCRIPTION:{Escape(string.Join("\n", notes))}";
        if (recipe is not null) yield return $"URL:{recipe}";
        yield return "TRANSP:TRANSPARENT";
        if (slowCooker && times.SlowCookerHours > 0 && meal.Status == MealStatus.Planned)
        {
            yield return "BEGIN:VALARM";
            yield return "ACTION:DISPLAY";
            yield return $"DESCRIPTION:{Escape($"Start the slow cooker for {meal.Title}.")}";
            yield return $"TRIGGER:-PT{times.SlowCookerHours}H";
            yield return "END:VALARM";
        }

        yield return "END:VEVENT";
    }

    private static TimeOnly StartOf(MealSlot slot, CalendarSettings times) => slot switch
    {
        MealSlot.Breakfast => CalendarSettings.Time(times.BreakfastTime, new TimeOnly(8, 0)),
        MealSlot.Lunch => CalendarSettings.Time(times.LunchTime, new TimeOnly(12, 30)),
        _ => CalendarSettings.Time(times.DinnerTime, new TimeOnly(18, 30)),
    };

    private static string Floating(DateTime time) => time.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
}
