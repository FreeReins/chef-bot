namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Asks for a new API token, named so its owner can tell it apart from their others.</summary>
public sealed record CreateApiTokenRequest(string Name);
