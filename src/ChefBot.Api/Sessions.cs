using System.Globalization;
using System.Security.Claims;
using ChefBot.Core.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ChefBot.Api;

/// <summary>Starts, ends and reads cookie sessions.</summary>
public static class Sessions
{
    private const string VersionClaim = "chefbot:session-version";

    public static Task SignInAsync(HttpContext http, User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(VersionClaim, user.SessionVersion.ToString(CultureInfo.InvariantCulture)),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    public static Task SignOutAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>The id of the signed-in account, or null when nobody is signed in.</summary>
    public static Guid? UserId(ClaimsPrincipal? principal) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>The id of the signed-in account.</summary>
    /// <exception cref="ChefBot.Core.Common.NotSignedInException">Nobody is signed in.</exception>
    public static Guid RequireUserId(ClaimsPrincipal? principal) =>
        UserId(principal) ?? throw new ChefBot.Core.Common.NotSignedInException("Sign in to continue.");

    /// <summary>
    /// True when the session was started for this account as it is now. A session stops being
    /// current when the account is removed or its password changes.
    /// </summary>
    public static bool IsCurrent(ClaimsPrincipal? principal, User? user) =>
        user is not null
        && principal?.FindFirstValue(VersionClaim) == user.SessionVersion.ToString(CultureInfo.InvariantCulture);
}
