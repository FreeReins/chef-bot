namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class PlannerSettingsEntity
{
    public int Id { get; set; } = 1;

    public int People { get; set; }

    public bool ScheduleEnabled { get; set; }

    public DayOfWeek ScheduleDay { get; set; }

    public string ScheduleTime { get; set; } = string.Empty;

    public int MaxLeftoverDays { get; set; }
}
