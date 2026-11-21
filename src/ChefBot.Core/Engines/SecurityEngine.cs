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

    public string LockedMessage(DateTimeOffset until, DateTimeOffset now) => LockedMessage(until - now);

    /// <summary>True when the rules lock accounts, so sign-in attempts need counting.</summary>
    public bool CountsSignIns(SecuritySettings rules) => rules.MaxFailedSignIns > 0;

    /// <summary>
    /// True when attempt <paramref name="number"/> is past the limit. That happens when several sign-ins arrive at
    /// once: the earlier ones use up the attempts the rules allow, so this one's password isn't checked at all.
    /// </summary>
    public bool IsPastLimit(int number, SecuritySettings rules) => CountsSignIns(rules) && number > rules.MaxFailedSignIns;

    /// <summary>When the account unlocks if attempt <paramref name="number"/> had a wrong password, or null if it stays unlocked.</summary>
    public DateTimeOffset? LockAfterWrongPassword(int number, SecuritySettings rules, DateTimeOffset now) =>
        CountsSignIns(rules) && number >= rules.MaxFailedSignIns ? now + TimeSpan.FromMinutes(rules.LockoutMinutes) : null;

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
