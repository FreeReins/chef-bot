namespace ChefBot.Core.Contracts.Messages;

/// <summary>An account that has just signed in, or been created or changed in a way that starts a new session.</summary>
public sealed record AccountSession(UserResponse User, SessionInfo Session);
