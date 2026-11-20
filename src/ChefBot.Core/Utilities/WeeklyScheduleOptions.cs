using System.Globalization;

namespace ChefBot.Core.Utilities;

/// <summary>When next week's plan is staged automatically. Bound from the "Schedule" settings section.</summary>
internal sealed class WeeklyScheduleOptions
{
    public bool Enabled { get; set; } = true;

    public DayOfWeek Day { get; set; } = DayOfWeek.Saturday;

    /// <summary>Local time of day, as HH:mm.</summary>
    public string Time { get; set; } = "09:00";

    /// <summary>The next moment the schedule fires, strictly after <paramref name="localNow"/>.</summary>
    public DateTimeOffset NextOccurrence(DateTimeOffset localNow)
    {
        var time = TimeOnly.TryParseExact(Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : new TimeOnly(9, 0);

        var daysAhead = ((int)Day - (int)localNow.DayOfWeek + 7) % 7;
        var candidate = new DateTimeOffset(localNow.Date.AddDays(daysAhead) + time.ToTimeSpan(), localNow.Offset);
        return candidate > localNow ? candidate : candidate.AddDays(7);
    }
}
