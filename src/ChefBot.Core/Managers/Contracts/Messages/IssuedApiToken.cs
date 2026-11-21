namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// A freshly created API token. <see cref="Token"/> is only available now, since it is stored as a hash. An
/// assistant sends it as <c>Authorization: Bearer {token}</c>.
/// </summary>
public sealed record IssuedApiToken(Guid Id, string Name, string Token, DateTimeOffset CreatedAt);
