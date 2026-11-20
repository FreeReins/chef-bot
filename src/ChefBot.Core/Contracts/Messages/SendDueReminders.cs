namespace ChefBot.Core.Contracts.Messages;

/// <summary>Command, every few minutes: send today's reminders if their time has come.</summary>
public sealed record SendDueReminders;
