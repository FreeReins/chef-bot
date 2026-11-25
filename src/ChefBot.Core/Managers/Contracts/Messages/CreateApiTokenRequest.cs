namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Asks for a new API token, named so its owner can tell it apart from their others.</summary>
/// <param name="Name">What to call it, such as the assistant it is for.</param>
/// <param name="ExpiresInDays">Days until it stops working (1 to 3650), or null for a token that lasts until it is revoked.</param>
/// <param name="ReadOnly">True for a token that can only use tools that change nothing, such as finding recipes.</param>
public sealed record CreateApiTokenRequest(string Name, int? ExpiresInDays = null, bool ReadOnly = false);
