namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One of the signed-in person's API tokens, without its secret, which can't be shown again.</summary>
public sealed record ApiTokenResponse(Guid Id, string Name, DateTimeOffset CreatedAt);
