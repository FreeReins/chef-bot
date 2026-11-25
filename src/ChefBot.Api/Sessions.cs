using System.Globalization;
using System.Security.Claims;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ChefBot.Api;

/// <summary>Starts, ends and reads cookie sessions.</summary>
public static class Sessions
{
    /// <summary>The authorization policy for endpoints only admins may use.</summary>
    public const string AdminPolicy = "Admin";

    /// <summary>The authorization policy for the MCP endpoint, which assistants sign in to with an API token.</summary>
    public const string McpPolicy = "Mcp";

    /// <summary>The role claim admins' sessions carry.</summary>
    public const string AdminRole = "admin";

    private const string VersionClaim = "chefbot:session-version";

    /// <summary>The claim a read-only API token's identity carries.</summary>
    private const string ReadOnlyClaim = "chefbot:read-only";

    public static Task SignInAsync(HttpContext http, SessionInfo session) =>
        http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, Principal(session));

    public static Task SignOutAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>The signed-in identity for an account as it is now.</summary>
    public static ClaimsPrincipal Principal(SessionInfo session) =>
        Principal(session, CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>
    /// The identity for an account as it is now, signed in with <paramref name="scheme"/>. A
    /// <paramref name="readOnly"/> one may only use MCP tools that change nothing.
    /// </summary>
    public static ClaimsPrincipal Principal(SessionInfo session, string scheme, bool readOnly = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.Username),
            new(VersionClaim, session.SessionVersion.ToString(CultureInfo.InvariantCulture)),
        };

        if (session.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, AdminRole));
        }

        if (readOnly)
        {
            claims.Add(new Claim(ReadOnlyClaim, "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }

    /// <summary>True when the identity came from a read-only API token.</summary>
    public static bool IsReadOnly(ClaimsPrincipal? principal) => principal?.HasClaim(ReadOnlyClaim, "true") ?? false;

    /// <summary>The id of the signed-in account, or null when nobody is signed in.</summary>
    public static Guid? UserId(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>The id of the signed-in account.</summary>
    /// <exception cref="NotSignedInException">Nobody is signed in.</exception>
    public static Guid RequireUserId(ClaimsPrincipal? principal) =>
        UserId(principal) ?? throw new NotSignedInException("Sign in to continue.");

    /// <summary>
    /// True when the session was started for this account as it is now. A session stops being
    /// current when the account is removed or its password changes.
    /// </summary>
    public static bool IsCurrent(ClaimsPrincipal? principal, SessionInfo? session) =>
        session is not null
        && principal?.FindFirstValue(VersionClaim) == session.SessionVersion.ToString(CultureInfo.InvariantCulture);

    /// <summary>True when the cookie's admin role no longer matches the account, so its claims need refreshing.</summary>
    public static bool AdminChanged(ClaimsPrincipal? principal, SessionInfo session) =>
        (principal?.IsInRole(AdminRole) ?? false) != session.IsAdmin;
}
