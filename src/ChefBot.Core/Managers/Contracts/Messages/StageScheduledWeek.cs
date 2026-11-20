namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Command: stage a week for the weekly schedule, once its Mealie import has finished or failed.</summary>
public sealed record StageScheduledWeek(DateOnly WeekStart);
