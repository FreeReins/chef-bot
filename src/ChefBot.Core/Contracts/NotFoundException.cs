namespace ChefBot.Core.Contracts;

/// <summary>The requested plan, meal, recipe or pantry item doesn't exist.</summary>
public sealed class NotFoundException(string message) : ChefBotException(message);
