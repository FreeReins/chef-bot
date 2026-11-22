namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Whether a notification reached the webhook.</summary>
/// <param name="Delivered">True when the webhook accepted it.</param>
/// <param name="Problem">What went wrong, when it wasn't delivered.</param>
/// <param name="Transient">
/// True when trying again later may work: the webhook couldn't be reached, timed out or had a server error. False
/// for an answer that won't change by waiting, such as a wrong address.
/// </param>
internal sealed record Delivery(bool Delivered, string? Problem = null, bool Transient = false);
