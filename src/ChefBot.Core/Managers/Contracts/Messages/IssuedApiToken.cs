namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// A freshly created API token. <see cref="Token"/> is only available now, since it is stored as a hash. An
/// assistant sends it as <c>Authorization: Bearer {token}</c>.
/// </summary>
/// <param name="Id">The token's id, for revoking it.</param>
/// <param name="Name">What its owner called it.</param>
/// <param name="Token">The secret itself.</param>
/// <param name="CreatedAt">When it was made.</param>
/// <param name="ExpiresAt">When it stops working, or null when it lasts until it is revoked.</param>
/// <param name="ReadOnly">True when it can only use tools that change nothing.</param>
public sealed record IssuedApiToken(
    Guid Id, string Name, string Token, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt = null, bool ReadOnly = false);
