using System.Security.Claims;
using ChefBot.Api.Contracts;
using ChefBot.Core.Accounts;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Managing who can use ChefBot. Every endpoint here is for admins only.</summary>
public static class PeopleEndpoints
{
    [WolverineGet("/api/users")]
    public static async Task<IReadOnlyList<UserResponse>> Users(
        ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        var users = await accounts.ListUsersAsync(cancellationToken);
        return users.Select(user => user.ToResponse()).ToList();
    }

    [WolverineDelete("/api/users/{id}")]
    public static async Task RemoveUser(
        Guid id, ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        var admin = await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        await accounts.RemoveUserAsync(id, admin.Id, cancellationToken);
    }

    /// <summary>Ends a lockout early.</summary>
    [WolverinePost("/api/users/{id}/unlock")]
    public static async Task<UserResponse> UnlockUser(
        Guid id, ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return (await accounts.UnlockUserAsync(id, cancellationToken)).ToResponse();
    }

    /// <summary>
    /// Creates a single-use password reset link for an account. Build it as
    /// <c>https://your-chefbot/?reset={token}</c>. The token is returned only here.
    /// </summary>
    [WolverinePost("/api/users/{id}/password-reset")]
    public static async Task<IssuedLink> CreatePasswordReset(
        Guid id, ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        var admin = await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await accounts.CreatePasswordResetAsync(id, admin.Id, cancellationToken);
    }

    /// <summary>
    /// Creates a single-use invite. Build the link as <c>https://your-chefbot/?invite={token}</c>.
    /// The token is returned only here.
    /// </summary>
    [WolverinePost("/api/invites")]
    public static async Task<IssuedLink> CreateInvite(
        ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        var admin = await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await accounts.CreateInviteAsync(admin.Id, cancellationToken);
    }

    /// <summary>Invite and reset links that haven't been used yet. Their tokens can't be shown again.</summary>
    [WolverineGet("/api/links")]
    public static async Task<IReadOnlyList<PendingLink>> Links(
        ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await accounts.ListLinksAsync(cancellationToken);
    }

    /// <summary>Replaces an unused link with a new one for the same purpose. The old link stops working.</summary>
    [WolverinePost("/api/links/{id}/regenerate")]
    public static async Task<IssuedLink> RegenerateLink(
        Guid id, ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        var admin = await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await accounts.RegenerateLinkAsync(id, admin.Id, cancellationToken);
    }

    [WolverineDelete("/api/links/{id}")]
    public static async Task RevokeLink(
        Guid id, ClaimsPrincipal principal, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        await accounts.RevokeLinkAsync(id, cancellationToken);
    }
}
