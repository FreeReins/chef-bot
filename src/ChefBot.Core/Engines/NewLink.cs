using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines;

/// <summary>A new link and the secret token that goes in it. The token is never stored, only its hash.</summary>
internal sealed record NewLink(AccessLink Link, string Token);
