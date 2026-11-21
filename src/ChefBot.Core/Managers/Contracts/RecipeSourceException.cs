namespace ChefBot.Core.Managers.Contracts;

/// <summary>A recipe page or photo on the web couldn't be downloaded.</summary>
public sealed class RecipeSourceException(string message, Exception? innerException = null)
    : ChefBotException(message, innerException);
