using System.Globalization;
using ChefBot.Core.Managers.Contracts;

namespace ChefBot.Core.Utilities;

/// <summary>Helpers for jobs that run once a day at a local time written as HH:mm.</summary>
internal static class DailyTime
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

    /// <summary>
    /// When to look next: now, if today's run is due; otherwise the next time the clock reaches <paramref name="time"/>
    /// (later today, or tomorrow), in UTC and to the whole second so it matches itself once saved.
    /// </summary>
    /// <param name="time">The time of day to run at, as HH:mm.</param>
    /// <param name="lastRunOn">The date of the last run.</param>
    /// <param name="utcNow">The current time.</param>
    /// <param name="zone">The household's time zone, which <paramref name="time"/> is in.</param>
    public static DateTimeOffset NextCheck(string time, DateOnly? lastRunOn, DateTimeOffset utcNow, TimeZoneInfo zone)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone);
        if (IsDue(time, lastRunOn, localNow))
        {
            return Seconds(utcNow);
        }

        var today = DateOnly.FromDateTime(localNow.DateTime);
        var at = Parse(time);
        var day = lastRunOn != today && TimeOnly.FromDateTime(localNow.DateTime) < at ? today : today.AddDays(1);
        var local = day.ToDateTime(at);
        return Seconds(new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime());
    }

    /// <summary>A time cut to the whole second, so it survives a round trip through the database unchanged.</summary>
    public static DateTimeOffset Seconds(DateTimeOffset value) => new(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Offset);
}
