namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A stretch of the food history, newest first.</summary>
/// <param name="Before">Only months before the one this date is in; null starts from the newest.</param>
/// <param name="Months">How many months that have something in them, from 1 to 24; 0 means 6.</param>
public sealed record HistoryRequest(DateOnly? Before, int Months);
