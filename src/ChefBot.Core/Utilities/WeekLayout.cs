namespace ChefBot.Core.Utilities;

/// <summary>The shape of a planning week, which runs Sunday to Saturday.</summary>
internal static class WeekLayout
{
    public static IReadOnlyList<DayOfWeek> Weekdays { get; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    /// <summary>The Sunday starting the next week to plan: today if it is Sunday, otherwise the coming one.</summary>
    public static DateOnly UpcomingWeekStart(DateOnly today) =>
        today.AddDays((7 - (int)today.DayOfWeek) % 7);

    public static DateOnly DateFor(DateOnly weekStart, DayOfWeek day) => weekStart.AddDays((int)day);

    public static DateOnly WeekEnd(DateOnly weekStart) => weekStart.AddDays(6);
}
