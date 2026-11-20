using System.Security.Claims;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Managing who can use ChefBot. Every endpoint here is for admins only.</summary>
[Authorize(Policy = Sessions.AdminPolicy)]
public static class PeopleEndpoints
{
    [WolverineGet("/api/users")]
    public static Task<IReadOnlyList<UserResponse>> Users([FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.ListUsersAsync(cancellationToken);

    [WolverineDelete("/api/users/{id}")]
    public static Task RemoveUser(Guid id, ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.RemoveUserAsync(id, Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>Ends a lockout early.</summary>
    [WolverinePost("/api/users/{id}/unlock")]
    public static Task<UserResponse> UnlockUser(Guid id, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.UnlockUserAsync(id, cancellationToken);

    /// <summary>
    /// Creates a single-use password reset link for an account. Build it as
    /// <c>https://your-chefbot/?reset={token}</c>. The token is returned only here.
    /// </summary>
    [WolverinePost("/api/users/{id}/password-reset")]
    public static Task<IssuedLink> CreatePasswordReset(
        Guid id, ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.CreatePasswordResetAsync(id, Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>
    /// Creates a single-use invite. Build the link as <c>https://your-chefbot/?invite={token}</c>.
    /// The token is returned only here.
    /// </summary>
    [WolverinePost("/api/invites")]
    public static Task<IssuedLink> CreateInvite(ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.CreateInviteAsync(Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>Invite and reset links that haven't been used yet. Their tokens can't be shown again.</summary>
    [WolverineGet("/api/links")]
    public static Task<IReadOnlyList<PendingLink>> Links([FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.ListLinksAsync(cancellationToken);

    /// <summary>Replaces an unused link with a new one for the same purpose. The old link stops working.</summary>
    [WolverinePost("/api/links/{id}/regenerate")]
    public static Task<IssuedLink> RegenerateLink(
        Guid id, ClaimsPrincipal principal, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.RegenerateLinkAsync(id, Sessions.RequireUserId(principal), cancellationToken);

    [WolverineDelete("/api/links/{id}")]
    public static Task RevokeLink(Guid id, [FromServices] IAccountManager accounts, CancellationToken cancellationToken) =>
        accounts.RevokeLinkAsync(id, cancellationToken);
}
