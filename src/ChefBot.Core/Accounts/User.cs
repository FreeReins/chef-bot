using System.Text.Json.Serialization;

namespace ChefBot.Core.Accounts;

/// <summary>
/// Someone who can sign in. Everyone shares the same recipes, plans and pantry;
/// accounts only decide who may use ChefBot and who may manage people.
/// </summary>
public sealed class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The sign-in name: lower-case and unique.</summary>
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>A salted hash. The password itself is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Admins can invite and remove people. The account created during setup is an admin.</summary>
    public bool IsAdmin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Wrong passwords in a row since the last successful sign-in or lockout.</summary>
    public int FailedSignIns { get; set; }

    /// <summary>Sign-in is refused until this time, after too many wrong passwords.</summary>
    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>Goes up whenever the password changes, which signs every older session out.</summary>
    public int SessionVersion { get; set; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil is DateTimeOffset until && until > now;

    /// <summary>Sets a new password hash, clears any lockout and ends existing sessions.</summary>
    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        SessionVersion++;
        Unlock();
    }

    public void Unlock()
    {
        FailedSignIns = 0;
        LockedUntil = null;
    }
}

/// <summary>What a single-use link lets its holder do.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LinkPurpose>))]
public enum LinkPurpose
{
    /// <summary>Create a new account.</summary>
    Invite,

    /// <summary>Choose a new password for an existing account.</summary>
    PasswordReset,
}

/// <summary>A single-use, expiring link: an invitation to join, or a password reset for one account.</summary>
public sealed class AccessLink
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public LinkPurpose Purpose { get; set; }

    /// <summary>A hash of the secret in the link, so a copy of the database can't be used to get in.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>The account a password reset is for. Null for invites.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The admin who created it. Null when it was created by the recovery setting at startup.</summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}

/// <summary>Sign-in rules an admin can change while ChefBot is running. There is exactly one row.</summary>
public sealed class SecuritySettings
{
    public int Id { get; set; } = 1;

    /// <summary>Wrong passwords in a row before an account is locked. Zero turns lockout off.</summary>
    public int MaxFailedSignIns { get; set; } = 5;

    /// <summary>How long a locked account stays locked.</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Send anyone arriving over plain HTTP to the HTTPS address.</summary>
    public bool RequireHttps { get; set; }
}
