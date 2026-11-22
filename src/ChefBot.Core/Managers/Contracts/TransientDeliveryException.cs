namespace ChefBot.Core.Managers.Contracts;

/// <summary>
/// An outside service, such as the notification webhook, couldn't be reached or answered with a server error. Thrown
/// from message handling so the message bus tries again later; never shown to a person.
/// </summary>
/// <param name="message">What went wrong, for the log.</param>
public sealed class TransientDeliveryException(string message) : Exception(message);
