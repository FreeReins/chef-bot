namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How many people plans are for, when next week is planned by itself, and how long leftovers keep.</summary>
/// <param name="People">People eating each breakfast, lunch and dinner.</param>
/// <param name="ScheduleEnabled">True when next week's plan is staged by itself each week.</param>
/// <param name="ScheduleDay">The day the weekly plan is staged.</param>
/// <param name="ScheduleTime">The local time it is staged, as HH:mm.</param>
/// <param name="MaxLeftoverDays">A lunch only uses a dinner cooked within this many days.</param>
/// <param name="NextStaging">When next week's plan will be staged by itself. Null when the schedule is off.</param>
/// <param name="IsSaved">False until these are first saved: until then they are the defaults.</param>
public sealed record PlannerSettingsResponse(
    int People,
    bool ScheduleEnabled,
    DayOfWeek ScheduleDay,
    string ScheduleTime,
    int MaxLeftoverDays,
    DateTimeOffset? NextStaging,
    bool IsSaved);
