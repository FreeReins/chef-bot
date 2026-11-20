namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>What the weekly schedule has done, so it stages each week only once.</summary>
internal sealed class ScheduleState
{
    /// <summary>The last week the schedule staged, or tried to stage.</summary>
    public DateOnly? LastStagedWeek { get; set; }
}
