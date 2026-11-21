using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A new API token and its secret. The secret is never stored, only its hash.</summary>
internal sealed record NewApiToken(ApiToken Token, string Secret);
