using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A new link and the secret token that goes in it. The token is never stored, only its hash.</summary>
internal sealed record NewLink(AccessLink Link, string Token);
