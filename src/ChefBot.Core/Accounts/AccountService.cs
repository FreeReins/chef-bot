using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;

namespace ChefBot.Core.Accounts;

/// <summary>The details someone enters to create an account.</summary>
public sealed record NewAccount(string Username, string DisplayName, string Password);

/// <summary>A freshly created link. The token is only available now: it is stored as a hash.</summary>
public sealed record IssuedLink(Guid Id, LinkPurpose Purpose, string Token, DateTimeOffset ExpiresAt);

/// <summary>A link that hasn't been used yet, as shown to admins. The token can't be shown again.</summary>
/// <param name="Id">The link's id.</param>
/// <param name="Purpose">Whether it is an invite or a password reset.</param>
/// <param name="Username">Whose password it resets. Null for invites.</param>
/// <param name="CreatedAt">When the link was created.</param>
/// <param name="ExpiresAt">When the link stops working.</param>
public sealed record PendingLink(Guid Id, LinkPurpose Purpose, string? Username, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

/// <summary>What a link is for, shown to the person who opened it.</summary>
public sealed record LinkDetails(LinkPurpose Purpose, string? Username, DateTimeOffset ExpiresAt);

/// <summary>First-run setup, signing in, passwords, lockout, and inviting or removing people.</summary>
public sealed class AccountService(
    IUserRepository users,
    IAccessLinkRepository links,
    ISettingsStore settings,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwords,
    TimeProvider clock)
{
    public const int MinimumPasswordLength = 8;

    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromDays(1);

    private const string WrongDetails = "That username and password don't match an account.";

    private static readonly Regex ValidUsername = new("^[a-z0-9._-]{3,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // ---------- setup and signing in ----------

    /// <summary>True until the first account exists, which is when the setup wizard is shown.</summary>
    public async Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken) =>
        !await users.AnyAsync(cancellationToken);

    /// <summary>Creates the first account, as an admin. Refused once any account exists.</summary>
    public async Task<User> CreateFirstAdminAsync(NewAccount account, CancellationToken cancellationToken)
    {
        if (await users.AnyAsync(cancellationToken))
        {
            throw new ConflictException("ChefBot is already set up. Sign in, or ask an admin for an invite link.");
        }

        var user = await BuildUserAsync(account, isAdmin: true, cancellationToken);
        users.Add(user);
        await unitOfWork.CommitAsync(cancellationToken);
        return user;
    }

    /// <summary>
    /// Checks a username and password. Wrong passwords are counted, and too many in a row
    /// lock the account for a while (see <see cref="SecuritySettings"/>).
    /// </summary>
    /// <exception cref="NotSignedInException">The details are wrong, or the account is locked.</exception>
    public async Task<User> SignInAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByUsernameAsync(Normalize(username), cancellationToken)
            ?? throw new NotSignedInException(WrongDetails);

        var now = clock.GetUtcNow();
        if (user.IsLocked(now))
        {
            throw new NotSignedInException(LockedMessage(user.LockedUntil!.Value - now));
        }

        if (passwords.Verify(user.PasswordHash, password ?? string.Empty))
        {
            if (user.FailedSignIns != 0 || user.LockedUntil is not null)
            {
                user.Unlock();
                await unitOfWork.CommitAsync(cancellationToken);
            }

            return user;
        }

        var rules = await settings.GetAsync<SecuritySettings>(cancellationToken);
        var message = WrongDetails;
        user.FailedSignIns++;
        if (rules.MaxFailedSignIns > 0 && user.FailedSignIns >= rules.MaxFailedSignIns)
        {
            var lockout = TimeSpan.FromMinutes(rules.LockoutMinutes);
            user.FailedSignIns = 0;
            user.LockedUntil = now + lockout;
            message = LockedMessage(lockout);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        throw new NotSignedInException(message);
    }

    public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken) => users.FindAsync(id, cancellationToken);

    /// <summary>The signed-in admin, for actions only admins may take.</summary>
    /// <exception cref="NotSignedInException">Nobody is signed in, or the account was removed.</exception>
    /// <exception cref="NotAllowedException">The account isn't an admin.</exception>
    public async Task<User> RequireAdminAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var user = userId is Guid id ? await users.FindAsync(id, cancellationToken) : null;
        if (user is null)
        {
            throw new NotSignedInException("Sign in to continue.");
        }

        return user.IsAdmin ? user : throw new NotAllowedException("Only an admin can do that.");
    }

    // ---------- passwords ----------

    /// <summary>Changes the signed-in person's own password. Their other sessions are signed out.</summary>
    public async Task<User> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        if (!passwords.Verify(user.PasswordHash, currentPassword ?? string.Empty))
        {
            throw new RuleViolationException("The current password isn't right.");
        }

        user.SetPassword(passwords.Hash(ValidPassword(newPassword)));
        await unitOfWork.CommitAsync(cancellationToken);
        return user;
    }

    /// <summary>
    /// Creates a single-use link that lets someone choose a new password for an account.
    /// Any earlier reset link for the same account stops working.
    /// </summary>
    /// <param name="userId">The account to reset.</param>
    /// <param name="createdByUserId">The admin asking, or null for the recovery setting at startup.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    public async Task<IssuedLink> CreatePasswordResetAsync(Guid userId, Guid? createdByUserId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        var existing = await links.ListAsync(cancellationToken);
        existing.Where(link => link.Purpose == LinkPurpose.PasswordReset && link.UserId == user.Id).ToList().ForEach(links.Remove);

        return await IssueAsync(LinkPurpose.PasswordReset, user.Id, createdByUserId, cancellationToken);
    }

    /// <summary>Creates a reset link for a username, for the recovery setting. Null when there is no such account.</summary>
    public async Task<IssuedLink?> CreatePasswordResetForUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var user = await users.FindByUsernameAsync(Normalize(username), cancellationToken);
        return user is null ? null : await CreatePasswordResetAsync(user.Id, null, cancellationToken);
    }

    /// <summary>Sets a new password from a reset link, uses the link up and unlocks the account.</summary>
    public async Task<User> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken)
    {
        var link = await GetUsableLinkAsync(token, LinkPurpose.PasswordReset, cancellationToken);
        var user = await GetUserAsync(link.UserId ?? Guid.Empty, cancellationToken);

        user.SetPassword(passwords.Hash(ValidPassword(newPassword)));
        links.Remove(link);
        await unitOfWork.CommitAsync(cancellationToken);
        return user;
    }

    // ---------- people ----------

    public async Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var all = await users.ListAsync(cancellationToken);
        return all.OrderBy(user => user.CreatedAt).ToList();
    }

    /// <summary>Removes an account and its reset links. Admins can't remove themselves, so one always remains.</summary>
    public async Task RemoveUserAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken)
    {
        if (id == actingUserId)
        {
            throw new RuleViolationException("You can't remove your own account.");
        }

        var user = await GetUserAsync(id, cancellationToken);
        var existing = await links.ListAsync(cancellationToken);
        existing.Where(link => link.UserId == user.Id).ToList().ForEach(links.Remove);
        users.Remove(user);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>Ends a lockout early.</summary>
    public async Task<User> UnlockUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(id, cancellationToken);
        user.Unlock();
        await unitOfWork.CommitAsync(cancellationToken);
        return user;
    }

    // ---------- links ----------

    /// <summary>Creates a single-use invite that expires after <see cref="InviteLifetime"/>.</summary>
    public Task<IssuedLink> CreateInviteAsync(Guid createdByUserId, CancellationToken cancellationToken) =>
        IssueAsync(LinkPurpose.Invite, userId: null, createdByUserId, cancellationToken);

    /// <summary>
    /// Replaces an unused link with a new one for the same purpose. The old link stops working,
    /// which is the way to get a link again after its one showing was missed.
    /// </summary>
    public async Task<IssuedLink> RegenerateLinkAsync(Guid id, Guid createdByUserId, CancellationToken cancellationToken)
    {
        var old = await links.FindAsync(id, cancellationToken) ?? throw new NotFoundException("That link no longer exists.");
        links.Remove(old);
        return await IssueAsync(old.Purpose, old.UserId, createdByUserId, cancellationToken);
    }

    /// <summary>Links that can still be used, newest first. Expired ones are cleaned up along the way.</summary>
    public async Task<IReadOnlyList<PendingLink>> ListLinksAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var all = await links.ListAsync(cancellationToken);
        var expired = all.Where(link => link.IsExpired(now)).ToList();
        if (expired.Count > 0)
        {
            expired.ForEach(links.Remove);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        var names = (await users.ListAsync(cancellationToken)).ToDictionary(user => user.Id, user => user.Username);
        return all.Except(expired)
            .OrderByDescending(link => link.CreatedAt)
            .Select(link => new PendingLink(
                link.Id, link.Purpose, link.UserId is Guid userId ? names.GetValueOrDefault(userId) : null, link.CreatedAt, link.ExpiresAt))
            .ToList();
    }

    public async Task RevokeLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        var link = await links.FindAsync(id, cancellationToken) ?? throw new NotFoundException("That link no longer exists.");
        links.Remove(link);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>What an opened link is for, so the page can show the right form.</summary>
    /// <exception cref="NotFoundException">The link was never valid, was already used, was replaced or has expired.</exception>
    public async Task<LinkDetails> DescribeLinkAsync(string token, CancellationToken cancellationToken)
    {
        var link = await GetUsableLinkAsync(token, purpose: null, cancellationToken);
        var user = link.UserId is Guid userId ? await users.FindAsync(userId, cancellationToken) : null;
        return new LinkDetails(link.Purpose, user?.Username, link.ExpiresAt);
    }

    /// <summary>Creates an account from an invite link and uses the invite up.</summary>
    public async Task<User> AcceptInviteAsync(string token, NewAccount account, CancellationToken cancellationToken)
    {
        var invite = await GetUsableLinkAsync(token, LinkPurpose.Invite, cancellationToken);
        var user = await BuildUserAsync(account, isAdmin: false, cancellationToken);
        users.Add(user);
        links.Remove(invite);
        await unitOfWork.CommitAsync(cancellationToken);
        return user;
    }

    // ---------- sign-in rules ----------

    public Task<SecuritySettings> GetSecuritySettingsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync<SecuritySettings>(cancellationToken);

    /// <summary>Changes the lockout rules and whether plain HTTP is redirected to HTTPS.</summary>
    public async Task<SecuritySettings> UpdateSecuritySettingsAsync(
        int maxFailedSignIns, int lockoutMinutes, bool requireHttps, CancellationToken cancellationToken)
    {
        if (maxFailedSignIns is < 0 or > 100)
        {
            throw new RuleViolationException("Failed attempts must be between 0 and 100. Zero turns lockout off.");
        }

        if (lockoutMinutes is < 1 or > 1440)
        {
            throw new RuleViolationException("The lockout must last between 1 minute and 24 hours.");
        }

        var rules = await settings.GetAsync<SecuritySettings>(cancellationToken);
        rules.MaxFailedSignIns = maxFailedSignIns;
        rules.LockoutMinutes = lockoutMinutes;
        rules.RequireHttps = requireHttps;
        await unitOfWork.CommitAsync(cancellationToken);
        return rules;
    }

    // ---------- helpers ----------

    private async Task<IssuedLink> IssueAsync(LinkPurpose purpose, Guid? userId, Guid? createdByUserId, CancellationToken cancellationToken)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var now = clock.GetUtcNow();
        var link = new AccessLink
        {
            Purpose = purpose,
            TokenHash = HashToken(token),
            UserId = userId,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = now + (purpose == LinkPurpose.Invite ? InviteLifetime : PasswordResetLifetime),
        };

        links.Add(link);
        await unitOfWork.CommitAsync(cancellationToken);
        return new IssuedLink(link.Id, purpose, token, link.ExpiresAt);
    }

    private async Task<AccessLink> GetUsableLinkAsync(string token, LinkPurpose? purpose, CancellationToken cancellationToken)
    {
        var link = string.IsNullOrWhiteSpace(token)
            ? null
            : await links.FindByTokenHashAsync(HashToken(token.Trim()), cancellationToken);

        var usable = link is not null && !link.IsExpired(clock.GetUtcNow()) && (purpose is null || link.Purpose == purpose);
        return usable ? link! : throw new NotFoundException("This link can't be used any more. Ask for a new one.");
    }

    private async Task<User> GetUserAsync(Guid id, CancellationToken cancellationToken) =>
        await users.FindAsync(id, cancellationToken) ?? throw new NotFoundException("That account no longer exists.");

    private async Task<User> BuildUserAsync(NewAccount account, bool isAdmin, CancellationToken cancellationToken)
    {
        var username = Normalize(account.Username);
        if (!ValidUsername.IsMatch(username))
        {
            throw new RuleViolationException("A username needs 3 to 32 characters: letters, numbers, dots, dashes or underscores.");
        }

        var password = ValidPassword(account.Password);
        if (await users.FindByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new ConflictException($"The username {username} is already taken.");
        }

        return new User
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(account.DisplayName) ? username : account.DisplayName.Trim(),
            PasswordHash = passwords.Hash(password),
            IsAdmin = isAdmin,
            CreatedAt = clock.GetUtcNow(),
        };
    }

    private static string ValidPassword(string? password) =>
        (password ?? string.Empty).Length >= MinimumPasswordLength
            ? password!
            : throw new RuleViolationException($"A password needs at least {MinimumPasswordLength} characters.");

    private static string LockedMessage(TimeSpan remaining) =>
        $"This account is locked after too many wrong passwords. Try again in {Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} minutes, or ask an admin to unlock it.";

    private static string Normalize(string? username) => (username ?? string.Empty).Trim().ToLowerInvariant();

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
