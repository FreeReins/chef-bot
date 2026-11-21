namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How sending to a to-do app went. Items are sent in order, so a problem stops the rest.</summary>
/// <param name="Sent">How many items the app took.</param>
/// <param name="Total">How many items there were to send.</param>
/// <param name="Problem">What went wrong, when not everything was sent.</param>
public sealed record SendShoppingListResult(int Sent, int Total, string? Problem = null);
