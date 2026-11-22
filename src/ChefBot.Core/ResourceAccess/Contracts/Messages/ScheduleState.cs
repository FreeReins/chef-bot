namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>What the weekly schedule has done, so it stages each week only once.</summary>
internal sealed class ScheduleState
{
    /// <summary>The last week the schedule staged, or tried to stage.</summary>
    public DateOnly? LastStagedWeek { get; set; }

    /// <summary>When the scheduled check for the weekly plan is due. A queued check due at another time is stale.</summary>
    public DateTimeOffset? WeeklyDueAt { get; set; }
}
