namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How many people plans are for, when next week is planned by itself, and how long leftovers keep.</summary>
/// <param name="People">People eating each breakfast, lunch and dinner, from 1 to 20. Weeks not yet confirmed are resized.</param>
/// <param name="ScheduleEnabled">True to stage next week's plan by itself each week.</param>
/// <param name="ScheduleDay">The day the weekly plan is staged.</param>
/// <param name="ScheduleTime">The local time it is staged, as HH:mm.</param>
/// <param name="MaxLeftoverDays">A lunch only uses a dinner cooked within this many days, from 0 to 7. Zero plans no leftover lunches.</param>
public sealed record PlannerSettingsRequest(int People, bool ScheduleEnabled, DayOfWeek ScheduleDay, string? ScheduleTime, int MaxLeftoverDays);
