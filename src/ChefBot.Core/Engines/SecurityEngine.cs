using System.Security.Cryptography;
using System.Text;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// The rules that keep accounts safe: counting wrong passwords and locking accounts, ending sessions when a
/// password changes, issuing and checking single-use links, and issuing API tokens. Reads and saves nothing.
/// </summary>
internal sealed class SecurityEngine
{
    public const string WrongDetails = "That username and password don't match an account.";

    /// <summary>How every API token's secret starts, so one pasted in the wrong place is easy to recognise.</summary>
    public const string ApiTokenPrefix = "chefbot_";

    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromDays(1);

    /// <summary>How often a token's last use is recorded: once in this long is enough to tell whether it is in use.</summary>
    public static readonly TimeSpan LastUsedPrecision = TimeSpan.FromMinutes(5);

    /// <summary>The longest an API token can be made to last, in days, short of lasting until it is revoked.</summary>
    public const int MaxApiTokenDays = 3650;

    public bool IsLocked(UserAccount user, DateTimeOffset now) => user.LockedUntil is DateTimeOffset until && until > now;

    /// <summary>
    /// What a refused sign-in says, whatever the reason: no such account, a wrong password or a locked account. It is
    /// the same for all three, so sign-in can't be used to find out which usernames exist. It explains the lockout
    /// rules, which are the same for every account.
    /// </summary>
    public string RefusedMessage(SecuritySettings rules) => CountsSignIns(rules)
        ? $"{WrongDetails} After {rules.MaxFailedSignIns} wrong passwords in a row an account is locked for {rules.LockoutMinutes} minutes, except on devices that have signed in to it before; an admin can unlock it sooner."
        : WrongDetails;

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
        EndSessions(user);
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

    /// <summary>
    /// A new API token for an account. Its secret starts with <see cref="ApiTokenPrefix"/>. It lasts
    /// <paramref name="days"/> days, or until it is revoked when that is null.
    /// </summary>
    public NewApiToken IssueApiToken(Guid userId, string name, int? days, bool readOnly, DateTimeOffset now)
    {
        var secret = ApiTokenPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var token = new ApiToken
        {
            UserId = userId,
            Name = name,
            TokenHash = HashToken(secret),
            CreatedAt = now,
            ExpiresAt = days is int d ? now.AddDays(d) : null,
            ReadOnly = readOnly,
        };
        return new NewApiToken(token, secret);
    }

    /// <summary>True when the token has an expiry date and it has passed.</summary>
    public bool IsExpired(ApiToken token, DateTimeOffset now) => token.ExpiresAt is DateTimeOffset until && now >= until;

    /// <summary>Ends the account's sessions everywhere: each sign-in cookie made before stops working.</summary>
    public void EndSessions(UserAccount user) => user.SessionVersion++;

    public string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    public bool IsExpired(AccessLink link, DateTimeOffset now) => now >= link.ExpiresAt;

    /// <summary>True when the link exists, hasn't expired and is for <paramref name="purpose"/> (any purpose when null).</summary>
    public bool IsUsable(AccessLink? link, LinkPurpose? purpose, DateTimeOffset now) =>
        link is not null && !IsExpired(link, now) && (purpose is null || link.Purpose == purpose);
}
