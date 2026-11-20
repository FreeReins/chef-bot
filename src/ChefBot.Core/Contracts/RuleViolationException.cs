namespace ChefBot.Core.Contracts;

/// <summary>The request or configuration breaks a rule, such as a recipe without a name.</summary>
public sealed class RuleViolationException(string message) : ChefBotException(message);
