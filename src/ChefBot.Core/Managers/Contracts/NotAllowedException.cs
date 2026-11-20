namespace ChefBot.Core.Managers.Contracts;

/// <summary>The caller is signed in but isn't allowed to do this.</summary>
public sealed class NotAllowedException(string message) : ChefBotException(message);
