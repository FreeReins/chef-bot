namespace ChefBot.Core.Managers.Contracts;

/// <summary>Base type for failures that are reported to the caller with a plain message.</summary>
public abstract class ChefBotException(string message, Exception? innerException = null)
    : Exception(message, innerException);
