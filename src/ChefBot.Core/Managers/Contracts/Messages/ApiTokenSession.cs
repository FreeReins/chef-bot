namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Who an API token signs in as, and what it may do.</summary>
/// <param name="Session">The token owner's account as it is now.</param>
/// <param name="ReadOnly">True when the token may only use tools that change nothing.</param>
public sealed record ApiTokenSession(SessionInfo Session, bool ReadOnly);
