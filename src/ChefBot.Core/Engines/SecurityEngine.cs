using System.Security.Cryptography;
using System.Text;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// The rules that keep accounts safe: counting wrong passwords and locking accounts, ending sessions when a
/// password changes, and issuing and checking single-use links. Reads and saves nothing.
/// </summary>
internal sealed class SecurityEngine
{
    public const string WrongDetails = "That username and password don't match an account.";

    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromDays(1);

    public bool IsLocked(UserAccount user, DateTimeOffset now) => user.LockedUntil is DateTimeOffset until && until > now;

    public string LockedMessage(UserAccount user, DateTimeOffset now) => LockedMessage(user.LockedUntil!.Value - now);

    /// <summary>Counts a wrong password and locks the account once there are too many in a row.</summary>
    /// <returns>What to tell the person signing in.</returns>
    public string RecordFailedSignIn(UserAccount user, SecuritySettings rules, DateTimeOffset now)
    {
        user.FailedSignIns++;
        if (rules.MaxFailedSignIns <= 0 || user.FailedSignIns < rules.MaxFailedSignIns)
        {
            return WrongDetails;
        }

        var lockout = TimeSpan.FromMinutes(rules.LockoutMinutes);
        user.FailedSignIns = 0;
        user.LockedUntil = now + lockout;
        return LockedMessage(lockout);
    }

    /// <summary>True when there is anything to clear after a successful sign-in.</summary>
    public bool NeedsUnlock(UserAccount user) => user.FailedSignIns != 0 || user.LockedUntil is not null;

    public void Unlock(UserAccount user)
    {
        user.FailedSignIns = 0;
        user.LockedUntil = null;
    }

    /// <summary>Sets a new password hash, clears any lockout and ends existing sessions.</summary>
    public void SetPassword(UserAccount user, string passwordHash)
    {
        user.PasswordHash = passwordHash;
        user.SessionVersion++;
        Unlock(user);
    }

    /// <summary>A new single-use link. Invites last <see cref="InviteLifetime"/>, resets <see cref="PasswordResetLifetime"/>.</summary>
    public NewLink IssueLink(LinkPurpose purpose, Guid? userId, Guid? createdByUserId, DateTimeOffset now)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var link = new AccessLink
        {
            Purpose = purpose,
            TokenHash = HashToken(token),
            UserId = userId,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = now + (purpose == LinkPurpose.Invite ? InviteLifetime : PasswordResetLifetime),
        };
        return new NewLink(link, token);
    }

    public string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    public bool IsExpired(AccessLink link, DateTimeOffset now) => now >= link.ExpiresAt;

    /// <summary>True when the link exists, hasn't expired and is for <paramref name="purpose"/> (any purpose when null).</summary>
    public bool IsUsable(AccessLink? link, LinkPurpose? purpose, DateTimeOffset now) =>
        link is not null && !IsExpired(link, now) && (purpose is null || link.Purpose == purpose);

    private static string LockedMessage(TimeSpan remaining) =>
        $"This account is locked after too many wrong passwords. Try again in {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} minutes, or ask an admin to unlock it.";
}
