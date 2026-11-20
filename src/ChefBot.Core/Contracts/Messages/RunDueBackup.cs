namespace ChefBot.Core.Contracts.Messages;

/// <summary>Command, every few minutes: run the daily backup if its time has come.</summary>
public sealed record RunDueBackup;
