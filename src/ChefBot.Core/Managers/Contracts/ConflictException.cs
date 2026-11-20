namespace ChefBot.Core.Managers.Contracts;

/// <summary>The request is valid but clashes with the current state, such as a week that is already confirmed.</summary>
public sealed class ConflictException(string message) : ChefBotException(message);
