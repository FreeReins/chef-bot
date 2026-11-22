using System.Globalization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <content>The weekly schedule: when next week's plan is staged by itself.</content>
internal sealed partial class PlanningEngine
{
    /// <summary>
    /// The week the weekly schedule should stage now, or null when it isn't due. The schedule is due from its day
    /// and time until the week it plans for starts, once per week: <paramref name="lastStagedWeek"/> records that.
    /// </summary>
    public static DateOnly? ScheduledWeekDue(PlannerSettings schedule, DateTimeOffset localNow, DateOnly? lastStagedWeek)
    {
        if (!schedule.ScheduleEnabled)
        {
            return null;
        }

        var lastFiring = NextOccurrence(schedule, localNow).AddDays(-7);
        var week = WeekLayout.UpcomingWeekStart(DateOnly.FromDateTime(lastFiring.DateTime));
        var today = DateOnly.FromDateTime(localNow.DateTime);

        // A firing that was missed (say the app was off) still counts until its week starts, but never later.
        return week == WeekLayout.UpcomingWeekStart(today) && week != lastStagedWeek ? week : null;
    }

    /// <summary>When the schedule next fires, or null when it is switched off.</summary>
    public static DateTimeOffset? NextScheduledStaging(PlannerSettings schedule, DateTimeOffset localNow) =>
        schedule.ScheduleEnabled ? NextOccurrence(schedule, localNow) : null;

    /// <summary>The next moment the schedule's day and time come round, strictly after <paramref name="localNow"/>.</summary>
    public static DateTimeOffset NextOccurrence(PlannerSettings schedule, DateTimeOffset localNow)
    {
        var time = TimeOnly.TryParseExact(schedule.ScheduleTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : new TimeOnly(9, 0);

        var daysAhead = ((int)schedule.ScheduleDay - (int)localNow.DayOfWeek + 7) % 7;
        var candidate = new DateTimeOffset(localNow.Date.AddDays(daysAhead) + time.ToTimeSpan(), localNow.Offset);
        return candidate > localNow ? candidate : candidate.AddDays(7);
    }
}
