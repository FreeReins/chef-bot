namespace ChefBot.Core.Contracts;

/// <summary>The caller isn't signed in, or the sign-in details were wrong.</summary>
public sealed class NotSignedInException(string message) : ChefBotException(message);
