using System.Globalization;

namespace ChefBot.Core.Utilities;

/// <summary>When meals appear in the calendar feed. Bound from the "Calendar" settings section. Times are local, as HH:mm.</summary>
internal sealed class CalendarOptions
{
    public string BreakfastTime { get; set; } = "08:00";

    public string LunchTime { get; set; } = "12:30";

    public string DinnerTime { get; set; } = "18:30";

    /// <summary>How long each meal's event lasts.</summary>
    public int MealMinutes { get; set; } = 45;

    /// <summary>How long before a slow cooker dinner its reminder to start the slow cooker goes off. Zero for none.</summary>
    public int SlowCookerHours { get; set; } = 8;

    /// <summary>The configured time of day, or <paramref name="fallback"/> when it isn't a valid HH:mm.</summary>
    public static TimeOnly Time(string? text, TimeOnly fallback) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : fallback;
}
