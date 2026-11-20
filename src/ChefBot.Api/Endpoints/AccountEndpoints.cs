using System.Security.Claims;
using ChefBot.Api.Contracts;
using ChefBot.Core.Accounts;
using ChefBot.Core.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>First-run setup, signing in and out, passwords, and joining with an invite link.</summary>
public static class AccountEndpoints
{
    /// <summary>Tells the page whether to show the setup wizard, the sign-in form or the app.</summary>
    [AllowAnonymous]
    [WolverineGet("/api/session")]
    public static async Task<SessionResponse> GetSession(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] RecipeImportService imports,
        CancellationToken cancellationToken)
    {
        var user = Sessions.UserId(principal) is Guid id ? await accounts.FindUserAsync(id, cancellationToken) : null;
        return new SessionResponse(
            await accounts.IsSetupRequiredAsync(cancellationToken), user?.ToResponse(), await imports.IsConfiguredAsync(cancellationToken));
    }

    /// <summary>Setup wizard, step one: creates the first account as an admin and signs it in.</summary>
    [AllowAnonymous]
    [WolverinePost("/api/setup/account")]
    public static async Task<UserResponse> CreateFirstAccount(
        NewAccount request, HttpContext http, [FromServices] AccountService accounts, CancellationToken cancellationToken) =>
        await StartSessionAsync(http, await accounts.CreateFirstAdminAsync(request, cancellationToken));

    /// <summary>Signs in. Too many wrong passwords in a row lock the account for a while.</summary>
    [AllowAnonymous]
    [WolverinePost("/api/account/sign-in")]
    public static async Task<UserResponse> SignIn(
        SignInRequest request, HttpContext http, [FromServices] AccountService accounts, CancellationToken cancellationToken) =>
        await StartSessionAsync(http, await accounts.SignInAsync(request.Username, request.Password, cancellationToken));

    [AllowAnonymous]
    [WolverinePost("/api/account/sign-out")]
    public static Task SignOut(HttpContext http) => Sessions.SignOutAsync(http);

    /// <summary>Says what an invite or password reset link is for, before showing its form.</summary>
    [AllowAnonymous]
    [WolverineGet("/api/account/link")]
    public static Task<LinkDetails> DescribeLink(
        string token, [FromServices] AccountService accounts, CancellationToken cancellationToken) =>
        accounts.DescribeLinkAsync(token, cancellationToken);

    /// <summary>Creates an account from an invite link, uses the invite up and signs the new account in.</summary>
    [AllowAnonymous]
    [WolverinePost("/api/account/accept-invite")]
    public static async Task<UserResponse> AcceptInvite(
        AcceptInviteRequest request, HttpContext http, [FromServices] AccountService accounts, CancellationToken cancellationToken)
    {
        var account = new NewAccount(request.Username, request.DisplayName, request.Password);
        return await StartSessionAsync(http, await accounts.AcceptInviteAsync(request.Token, account, cancellationToken));
    }

    /// <summary>Sets a new password from a reset link, unlocks the account and signs it in.</summary>
    [AllowAnonymous]
    [WolverinePost("/api/account/reset-password")]
    public static async Task<UserResponse> ResetPassword(
        ResetPasswordRequest request, HttpContext http, [FromServices] AccountService accounts, CancellationToken cancellationToken) =>
        await StartSessionAsync(http, await accounts.ResetPasswordAsync(request.Token, request.Password, cancellationToken));

    /// <summary>Changes your own password. Other devices are signed out; this one stays signed in.</summary>
    [WolverinePost("/api/account/password")]
    public static async Task<UserResponse> ChangePassword(
        ChangePasswordRequest request,
        HttpContext http,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        CancellationToken cancellationToken)
    {
        var userId = Sessions.RequireUserId(principal);
        var user = await accounts.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return await StartSessionAsync(http, user);
    }

    private static async Task<UserResponse> StartSessionAsync(HttpContext http, User user)
    {
        await Sessions.SignInAsync(http, user);
        return user.ToResponse();
    }
}
