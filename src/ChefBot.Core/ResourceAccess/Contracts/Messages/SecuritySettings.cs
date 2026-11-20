namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Sign-in rules an admin can change while ChefBot is running.</summary>
internal sealed class SecuritySettings
{
    /// <summary>Wrong passwords in a row before an account is locked. Zero turns lockout off.</summary>
    public int MaxFailedSignIns { get; set; } = 5;

    /// <summary>How long a locked account stays locked.</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Send anyone arriving over plain HTTP to the HTTPS address.</summary>
    public bool RequireHttps { get; set; }
}
