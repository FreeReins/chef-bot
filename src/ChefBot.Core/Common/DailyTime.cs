using System.Globalization;

namespace ChefBot.Core.Common;

/// <summary>Helpers for jobs that run once a day at a local time written as HH:mm.</summary>
public static class DailyTime
{
    /// <exception cref="RuleViolationException">The text isn't a time of day.</exception>
    public static TimeOnly Parse(string? text) =>
        TimeOnly.TryParseExact(text?.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : throw new RuleViolationException("Times must be written as HH:mm, for example 07:30.");

    public static string Format(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>True when today's run hasn't happened yet and its time has come.</summary>
    /// <param name="time">The time of day to run at, as HH:mm.</param>
    /// <param name="lastRunOn">The date of the last run.</param>
    /// <param name="localNow">The current local time.</param>
    public static bool IsDue(string time, DateOnly? lastRunOn, DateTimeOffset localNow)
    {
        var today = DateOnly.FromDateTime(localNow.DateTime);
        return lastRunOn != today && TimeOnly.FromDateTime(localNow.DateTime) >= Parse(time);
    }
}
