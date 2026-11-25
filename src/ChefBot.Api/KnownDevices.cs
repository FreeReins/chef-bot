using Microsoft.AspNetCore.DataProtection;

namespace ChefBot.Api;

/// <summary>
/// Remembers, in an encrypted cookie, which account a browser has signed in to. Account lockout doesn't apply to such
/// a device, so someone guessing passwords from elsewhere can't keep the owner out. The cookie only lets its device
/// try a password; it signs nobody in.
/// </summary>
public static class KnownDevices
{
    /// <summary>The cookie's name.</summary>
    public const string CookieName = "chefbot.device";

    /// <summary>How long a device is remembered after it last signed in.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    private const string Purpose = "ChefBot.KnownDevice.v1";

    /// <summary>The account this browser signed in to before, or null when it has no valid cookie.</summary>
    public static Guid? Read(HttpContext http)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName, out var value) || string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return Guid.TryParse(Protector(http).Unprotect(value), out var userId) ? userId : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Remembers that this browser signed in to <paramref name="userId"/>, for <see cref="Lifetime"/>.</summary>
    public static void Remember(HttpContext http, Guid userId) =>
        http.Response.Cookies.Append(CookieName, Protector(http).Protect(userId.ToString()), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = http.Request.IsHttps,
            Path = "/api/account",
            MaxAge = Lifetime,
            IsEssential = true,
        });

    private static IDataProtector Protector(HttpContext http) =>
        http.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);
}
