namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Someone who can sign in. Everyone shares the same recipes, plans and pantry.</summary>
internal sealed class UserAccount
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
}
