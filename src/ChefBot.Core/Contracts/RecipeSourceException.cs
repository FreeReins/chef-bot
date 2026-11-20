namespace ChefBot.Core.Contracts;

/// <summary>An outside recipe source such as Mealie couldn't be read.</summary>
public sealed class RecipeSourceException(string message, Exception? innerException = null)
    : ChefBotException(message, innerException);
