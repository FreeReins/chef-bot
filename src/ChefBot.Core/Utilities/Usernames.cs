namespace ChefBot.Core.Utilities;

internal static class Usernames
{
    /// <summary>Sign-in names are compared trimmed and lower-case.</summary>
    public static string Normalize(string? username) => (username ?? string.Empty).Trim().ToLowerInvariant();
}
