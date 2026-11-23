using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IAccountManager"/>
internal sealed partial class AccountManager(
    IAccountAccess accounts,
    IInboxAccess inbox,
    ISettingsAccess settings,
    IFavouriteAccess favourites,
    IMessageAccess messages,
    ValidationEngine validation,
    SecurityEngine security,
    IPasswordHasher passwords,
    TimeProvider clock) : IAccountManager
{
    public async Task<SessionResponse> DescribeSessionAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var user = userId is Guid id ? await accounts.FindUserAsync(id, cancellationToken) : null;
        return new SessionResponse(!await accounts.AnyUsersAsync(cancellationToken), user?.ToResponse());
    }

    public async Task<SessionInfo?> FindSessionAsync(Guid userId, CancellationToken cancellationToken) =>
        (await accounts.FindUserAsync(userId, cancellationToken))?.ToSession();

    public async Task<AccountSession> CreateFirstAdminAsync(NewAccount account, CancellationToken cancellationToken)
    {
        await validation.SetupIsOpenAsync(cancellationToken);
        var user = await BuildUserAsync(account, isAdmin: true, cancellationToken);
        await accounts.SaveUserAsync(user, cancellationToken);
        await messages.PublishAsync(new KitchenOpened(user.Id, user.DisplayName, clock.Today(), clock.GetUtcNow()));
        await messages.CommitAsync(cancellationToken);
        return user.ToAccountSession();
    }

    public async Task<AccountSession> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var user = await accounts.FindUserByUsernameAsync(Usernames.Normalize(request.Username), cancellationToken)
            ?? throw new NotSignedInException(SecurityEngine.WrongDetails);

        var now = clock.GetUtcNow();
        if (security.IsLocked(user, now))
        {
            throw new NotSignedInException(security.LockedMessage(user, now));
        }

        var password = request.Password ?? string.Empty;
        var rules = await settings.GetSecurityAsync(cancellationToken);
        if (!security.CountsSignIns(rules))
        {
            return passwords.Verify(user.PasswordHash, password)
                ? user.ToAccountSession()
                : throw new NotSignedInException(SecurityEngine.WrongDetails);
        }

        // The attempt is counted in the database before the password is checked, so many sign-ins sent at once
        // can't check more passwords between them than the rules allow. These counters don't wait for a commit.
        var attempt = await accounts.CountSignInAttemptAsync(user.Id, now, cancellationToken);
        if (attempt.Number is not int number)
        {
            throw new NotSignedInException(attempt.LockedUntil is DateTimeOffset until
                ? security.LockedMessage(until, now)
                : SecurityEngine.WrongDetails);
        }

        if (security.IsPastLimit(number, rules))
        {
            throw new NotSignedInException(security.LockedMessage(now + TimeSpan.FromMinutes(rules.LockoutMinutes), now));
        }

        if (passwords.Verify(user.PasswordHash, password))
        {
            await accounts.ClearSignInAttemptsAsync(user.Id, cancellationToken);
            return user.ToAccountSession();
        }

        if (security.LockAfterWrongPassword(number, rules, now) is DateTimeOffset lockedUntil)
        {
            await accounts.LockAsync(user.Id, lockedUntil, cancellationToken);
            throw new NotSignedInException(security.LockedMessage(lockedUntil, now));
        }

        throw new NotSignedInException(SecurityEngine.WrongDetails);
    }

    public async Task<LinkDetails> DescribeLinkAsync(string token, CancellationToken cancellationToken)
    {
        var link = await UsableLinkAsync(token, purpose: null, cancellationToken);
        var user = link.UserId is Guid userId ? await accounts.FindUserAsync(userId, cancellationToken) : null;
        return new LinkDetails(link.Purpose, user?.Username, link.ExpiresAt);
    }

    public async Task<AccountSession> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken cancellationToken)
    {
        var invite = await UsableLinkAsync(request.Token, LinkPurpose.Invite, cancellationToken);
        var user = await BuildUserAsync(new NewAccount(request.Username, request.DisplayName, request.Password), isAdmin: false, cancellationToken);
        await accounts.SaveUserAsync(user, cancellationToken);
        await accounts.RemoveLinkAsync(invite.Id, cancellationToken);
        await messages.PublishAsync(new MemberJoined(user.Id, user.DisplayName, clock.Today(), clock.GetUtcNow()));
        await messages.CommitAsync(cancellationToken);
        return user.ToAccountSession();
    }

    public async Task<AccountSession> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var link = await UsableLinkAsync(request.Token, LinkPurpose.PasswordReset, cancellationToken);
        var user = await validation.UserAsync(link.UserId ?? Guid.Empty, cancellationToken);

        security.SetPassword(user, passwords.Hash(ValidationEngine.Password(request.Password)));
        await accounts.SaveUserAsync(user, cancellationToken);
        await accounts.RemoveLinkAsync(link.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return user.ToAccountSession();
    }

    public async Task<AccountSession> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await validation.UserAsync(userId, cancellationToken);
        if (!passwords.Verify(user.PasswordHash, request.CurrentPassword ?? string.Empty))
        {
            throw new RuleViolationException("The current password isn't right.");
        }

        security.SetPassword(user, passwords.Hash(ValidationEngine.Password(request.NewPassword)));
        await accounts.SaveUserAsync(user, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return user.ToAccountSession();
    }

    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken) =>
        (await accounts.ListUsersAsync(cancellationToken)).OrderBy(user => user.CreatedAt).Select(user => user.ToResponse()).ToList();

    public async Task RemoveUserAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken)
    {
        ValidationEngine.CanRemove(id, actingUserId);
        var user = await validation.UserAsync(id, cancellationToken);
        await RemoveLinksAsync(link => link.UserId == user.Id, cancellationToken);
        foreach (var token in await accounts.ListApiTokensAsync(user.Id, cancellationToken))
        {
            await accounts.RemoveApiTokenAsync(token.Id, cancellationToken);
        }

        await favourites.RemoveForUserAsync(user.Id, cancellationToken);
        await inbox.ClearAsync(user.Id, cancellationToken);
        await accounts.RemoveUserAsync(user.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<UserResponse> UnlockUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await validation.UserAsync(id, cancellationToken);
        security.Unlock(user);
        await accounts.SaveUserAsync(user, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task<IssuedLink> CreatePasswordResetAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken)
    {
        var user = await validation.UserAsync(id, cancellationToken);
        return await IssueResetAsync(user, actingUserId, cancellationToken);
    }

    public async Task<IssuedLink?> CreateRecoveryLinkAsync(string username, CancellationToken cancellationToken)
    {
        var user = await accounts.FindUserByUsernameAsync(Usernames.Normalize(username), cancellationToken);
        return user is null ? null : await IssueResetAsync(user, createdByUserId: null, cancellationToken);
    }

    public Task<IssuedLink> CreateInviteAsync(Guid actingUserId, CancellationToken cancellationToken) =>
        IssueAsync(LinkPurpose.Invite, userId: null, actingUserId, cancellationToken);

    public async Task<IReadOnlyList<PendingLink>> ListLinksAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var all = await accounts.ListLinksAsync(cancellationToken);
        var expired = all.Where(link => security.IsExpired(link, now)).ToList();
        if (expired.Count > 0)
        {
            foreach (var link in expired)
            {
                await accounts.RemoveLinkAsync(link.Id, cancellationToken);
            }

            await messages.CommitAsync(cancellationToken);
        }

        var names = (await accounts.ListUsersAsync(cancellationToken)).ToDictionary(user => user.Id, user => user.Username);
        return all.Except(expired)
            .OrderByDescending(link => link.CreatedAt)
            .Select(link => new PendingLink(
                link.Id, link.Purpose, link.UserId is Guid userId ? names.GetValueOrDefault(userId) : null, link.CreatedAt, link.ExpiresAt))
            .ToList();
    }

    public async Task<IssuedLink> RegenerateLinkAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken)
    {
        var old = await validation.LinkAsync(id, cancellationToken);
        await accounts.RemoveLinkAsync(old.Id, cancellationToken);
        return await IssueAsync(old.Purpose, old.UserId, actingUserId, cancellationToken);
    }

    public async Task RevokeLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        var link = await validation.LinkAsync(id, cancellationToken);
        await accounts.RemoveLinkAsync(link.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<SecurityResponse> GetSecurityAsync(CancellationToken cancellationToken) =>
        (await settings.GetSecurityAsync(cancellationToken)).ToResponse();

    public async Task<SecurityResponse> UpdateSecurityAsync(SecurityRequest request, CancellationToken cancellationToken)
    {
        ValidationEngine.SignInRules(request);
        var rules = await settings.GetSecurityAsync(cancellationToken);
        rules.MaxFailedSignIns = request.MaxFailedSignIns;
        rules.LockoutMinutes = request.LockoutMinutes;
        rules.RequireHttps = request.RequireHttps;
        await settings.SaveSecurityAsync(rules, cancellationToken);
        await messages.PublishAsync(new SecuritySettingsChanged(rules.RequireHttps));
        await messages.CommitAsync(cancellationToken);
        return rules.ToResponse();
    }

    public async Task<SecurityResponse> UseDefaultHttpsRuleAsync(bool servesHttps, CancellationToken cancellationToken)
    {
        var rules = await settings.GetSecurityAsync(cancellationToken);
        if (servesHttps && !rules.IsSaved)
        {
            rules.RequireHttps = true;
            await settings.SaveSecurityAsync(rules, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }

        return rules.ToResponse();
    }

    public async Task<IReadOnlyList<ApiTokenResponse>> ListApiTokensAsync(Guid userId, CancellationToken cancellationToken) =>
        (await accounts.ListApiTokensAsync(userId, cancellationToken))
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => new ApiTokenResponse(token.Id, token.Name, token.CreatedAt))
            .ToList();

    public async Task<IssuedApiToken> CreateApiTokenAsync(Guid userId, CreateApiTokenRequest request, CancellationToken cancellationToken)
    {
        var name = ValidationEngine.ApiTokenName(request.Name);
        var user = await validation.UserAsync(userId, cancellationToken);
        var issued = security.IssueApiToken(user.Id, name, clock.GetUtcNow());
        await accounts.SaveApiTokenAsync(issued.Token, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return new IssuedApiToken(issued.Token.Id, issued.Token.Name, issued.Secret, issued.Token.CreatedAt);
    }

    public async Task RevokeApiTokenAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var token = (await accounts.ListApiTokensAsync(userId, cancellationToken)).FirstOrDefault(token => token.Id == id)
            ?? throw new NotFoundException("That token no longer exists.");
        await accounts.RemoveApiTokenAsync(token.Id, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<SessionInfo?> FindApiTokenSessionAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var stored = await accounts.FindApiTokenByHashAsync(security.HashToken(token), cancellationToken);
        return stored is null ? null : (await accounts.FindUserAsync(stored.UserId, cancellationToken))?.ToSession();
    }

    /// <summary>A new password reset link for an account. Any earlier reset link for it stops working.</summary>
    private async Task<IssuedLink> IssueResetAsync(UserAccount user, Guid? createdByUserId, CancellationToken cancellationToken)
    {
        await RemoveLinksAsync(link => link.Purpose == LinkPurpose.PasswordReset && link.UserId == user.Id, cancellationToken);
        return await IssueAsync(LinkPurpose.PasswordReset, user.Id, createdByUserId, cancellationToken);
    }

    private async Task<IssuedLink> IssueAsync(LinkPurpose purpose, Guid? userId, Guid? createdByUserId, CancellationToken cancellationToken)
    {
        var issued = security.IssueLink(purpose, userId, createdByUserId, clock.GetUtcNow());
        await accounts.SaveLinkAsync(issued.Link, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return new IssuedLink(issued.Link.Id, purpose, issued.Token, issued.Link.ExpiresAt);
    }

    private async Task RemoveLinksAsync(Func<AccessLink, bool> which, CancellationToken cancellationToken)
    {
        foreach (var link in (await accounts.ListLinksAsync(cancellationToken)).Where(which))
        {
            await accounts.RemoveLinkAsync(link.Id, cancellationToken);
        }
    }

    /// <exception cref="NotFoundException">The link was never valid, was already used, was replaced or has expired.</exception>
    private async Task<AccessLink> UsableLinkAsync(string token, LinkPurpose? purpose, CancellationToken cancellationToken)
    {
        var link = string.IsNullOrWhiteSpace(token)
            ? null
            : await accounts.FindLinkByTokenHashAsync(security.HashToken(token), cancellationToken);

        return security.IsUsable(link, purpose, clock.GetUtcNow())
            ? link!
            : throw new NotFoundException("This link can't be used any more. Ask for a new one.");
    }

    private async Task<UserAccount> BuildUserAsync(NewAccount account, bool isAdmin, CancellationToken cancellationToken)
    {
        var username = await validation.NewAccountAsync(account, cancellationToken);
        return new UserAccount
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(account.DisplayName) ? username : account.DisplayName.Trim(),
            PasswordHash = passwords.Hash(account.Password),
            IsAdmin = isAdmin,
            CreatedAt = clock.GetUtcNow(),
        };
    }
}
