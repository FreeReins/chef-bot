using System.Security.Claims;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The signed-in person's API tokens, which assistants use to sign in to the MCP endpoint at /mcp.</summary>
public static class ApiTokenEndpoints
{
    /// <summary>Your API tokens, newest first. Their secrets can't be shown again.</summary>
    [WolverineGet("/api/tokens")]
    public static Task<IReadOnlyList<ApiTokenResponse>> Tokens(
        ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.ListApiTokensAsync(Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>
    /// Creates an API token that signs in as you. Send it as <c>Authorization: Bearer {token}</c>. The token is
    /// returned only here.
    /// </summary>
    [WolverinePost("/api/tokens")]
    public static Task<IssuedApiToken> CreateToken(
        CreateApiTokenRequest request, ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.CreateApiTokenAsync(Sessions.RequireUserId(principal), request, cancellationToken);

    [WolverineDelete("/api/tokens/{id}")]
    public static Task RevokeToken(Guid id, ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.RevokeApiTokenAsync(Sessions.RequireUserId(principal), id, cancellationToken);
}
