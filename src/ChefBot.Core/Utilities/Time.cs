using System.Globalization;
using ChefBot.Core.Contracts;

namespace ChefBot.Core.Utilities;

internal static class ClockExtensions
{
    /// <summary>Today's date in the server's time zone (set with the TZ environment variable).</summary>
    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}

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
}

/// <summary>The shape of a planning week, which runs Sunday to Saturday.</summary>
internal static class WeekLayout
{
    private static readonly DayOfWeek[] ThreeDinners = [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday];
    private static readonly DayOfWeek[] FourDinners = [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday];
    private static readonly DayOfWeek[] FiveDinners = [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday];

    public static IReadOnlyList<DayOfWeek> Weekdays { get; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    /// <summary>The Sunday starting the next week to plan: today if it is Sunday, otherwise the coming one.</summary>
    public static DateOnly UpcomingWeekStart(DateOnly today) =>
        today.AddDays((7 - (int)today.DayOfWeek) % 7);

    public static DateOnly DateFor(DateOnly weekStart, DayOfWeek day) => weekStart.AddDays((int)day);

    public static DateOnly WeekEnd(DateOnly weekStart) => weekStart.AddDays(6);

    /// <summary>The cooking days, spaced so each weekday lunch can be leftovers from a recent dinner.</summary>
    public static IReadOnlyList<DayOfWeek> DinnerDays(int dinners) => dinners switch
    {
        3 => ThreeDinners,
        4 => FourDinners,
        5 => FiveDinners,
        _ => throw new RuleViolationException("Planner:Dinners must be 3, 4 or 5."),
    };
}

/// <summary>
/// Lets one change to a plan finish before the next one starts. Two refreshes of the same plan at once (the web
/// page's own refresh and the one that follows a pantry change) would otherwise both replace its shopping list.
/// ChefBot runs as a single instance per database, so a lock in memory is enough.
/// </summary>
internal sealed class PlanWriteLock
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Release(_gate);
    }

    private sealed class Release(SemaphoreSlim gate) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
