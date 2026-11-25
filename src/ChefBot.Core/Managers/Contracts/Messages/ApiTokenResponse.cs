namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One of the signed-in person's API tokens, without its secret, which can't be shown again.</summary>
/// <param name="Id">The token's id, for revoking it.</param>
/// <param name="Name">What its owner called it.</param>
/// <param name="CreatedAt">When it was made.</param>
/// <param name="ExpiresAt">When it stops working, or null when it lasts until it is revoked.</param>
/// <param name="LastUsedAt">When an assistant last used it, to the nearest few minutes, or null when never.</param>
/// <param name="ReadOnly">True when it can only use tools that change nothing.</param>
public sealed record ApiTokenResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt = null,
    DateTimeOffset? LastUsedAt = null,
    bool ReadOnly = false);
