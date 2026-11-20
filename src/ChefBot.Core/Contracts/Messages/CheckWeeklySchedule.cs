namespace ChefBot.Core.Contracts.Messages;

/// <summary>Command, every few minutes: stage next week's plan if the weekly schedule's time has come.</summary>
public sealed record CheckWeeklySchedule;
