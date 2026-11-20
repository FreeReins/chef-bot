namespace ChefBot.Core.Contracts;

/// <summary>Base type for failures that are reported to the caller with a plain message.</summary>
public abstract class ChefBotException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>The requested plan, meal, recipe or pantry item doesn't exist.</summary>
public sealed class NotFoundException(string message) : ChefBotException(message);

/// <summary>The request is valid but clashes with the current state, such as a week that is already confirmed.</summary>
public sealed class ConflictException(string message) : ChefBotException(message);

/// <summary>The request or configuration breaks a rule, such as a recipe without a name.</summary>
public sealed class RuleViolationException(string message) : ChefBotException(message);

/// <summary>An outside recipe source such as Mealie couldn't be read.</summary>
public sealed class RecipeSourceException(string message, Exception? innerException = null)
    : ChefBotException(message, innerException);

/// <summary>The caller isn't signed in, or the sign-in details were wrong.</summary>
public sealed class NotSignedInException(string message) : ChefBotException(message);

/// <summary>The caller is signed in but isn't allowed to do this.</summary>
public sealed class NotAllowedException(string message) : ChefBotException(message);
