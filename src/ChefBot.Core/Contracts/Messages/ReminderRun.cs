namespace ChefBot.Core.Contracts.Messages;

/// <summary>The outcome of sending a batch of reminders.</summary>
/// <param name="Sent">The types that were delivered.</param>
/// <param name="Problems">What went wrong for those that weren't.</param>
public sealed record ReminderRun(IReadOnlyList<string> Sent, IReadOnlyList<string> Problems);
