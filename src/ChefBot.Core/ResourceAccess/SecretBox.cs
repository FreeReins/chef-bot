using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Encrypts secrets ChefBot has to send on, such as AI providers' API keys, before they are stored, with the same keys
/// that protect sign-in cookies (Data:SessionKeys). A copy of the database or a backup alone doesn't give them away.
/// </summary>
internal sealed class SecretBox(IDataProtectionProvider provider)
{
    /// <summary>How a stored value starts once it is encrypted. Anything else was stored before encryption.</summary>
    public const string Prefix = "dp1:";

    private readonly IDataProtector _protector = provider.CreateProtector("ChefBot.StoredSecrets.v1");

    /// <summary>The value to store: empty stays empty, anything else is encrypted.</summary>
    public string Seal(string secret) => secret.Length == 0 ? secret : Prefix + _protector.Protect(secret);

    /// <summary>True when the stored value is plain text from before secrets were encrypted.</summary>
    public static bool IsPlain(string stored) => stored.Length > 0 && !stored.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>
    /// The secret in a stored value, or null when it can't be read: it was sealed by another ChefBot, whose keys this
    /// one doesn't have, such as after restoring a backup on a new computer.
    /// </summary>
    public string? Open(string stored)
    {
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return stored;
        }

        try
        {
            return _protector.Unprotect(stored[Prefix.Length..]);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
