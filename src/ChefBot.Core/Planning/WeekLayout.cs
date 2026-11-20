using ChefBot.Core.Common;

namespace ChefBot.Core.Planning;

/// <summary>The shape of a planning week, which runs Sunday to Saturday.</summary>
public static class WeekLayout
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

    /// <summary>The cooking days, spaced so each weekday lunch can be leftovers from a recent dinner.</summary>
    public static IReadOnlyList<DayOfWeek> DinnerDays(int dinners) => dinners switch
    {
        3 => ThreeDinners,
        4 => FourDinners,
        5 => FiveDinners,
        _ => throw new RuleViolationException("Planner:Dinners must be 3, 4 or 5."),
    };
}
