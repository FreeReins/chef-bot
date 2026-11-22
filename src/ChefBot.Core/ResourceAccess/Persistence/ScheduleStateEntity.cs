namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class ScheduleStateEntity
{
    public int Id { get; set; } = 1;

    public DateOnly? LastStagedWeek { get; set; }

    public DateTimeOffset? WeeklyDueAt { get; set; }
}
