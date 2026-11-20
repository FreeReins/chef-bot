using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;
using ChefBot.Core.Engines;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IAccountManager"/>
internal sealed class AccountManager(
    IAccountAccess accounts,
    ISettingsAccess settings,
    IUnitOfWork unitOfWork,
    ValidationEngine validation,
    SecurityEngine security,
    IPasswordHasher passwords,
    TimeProvider clock) : IAccountManager
{
    public async Task<SessionResponse> DescribeSessionAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var user = userId is Guid id ? await accounts.FindUserAsync(id, cancellationToken) : null;
        return new SessionResponse(
            !await accounts.AnyUsersAsync(cancellationToken),
            user?.ToResponse(),
            (await settings.GetMealieAsync(cancellationToken)).IsConfigured);
    }

    public async Task<SessionInfo?> FindSessionAsync(Guid userId, CancellationToken cancellationToken) =>
        (await accounts.FindUserAsync(userId, cancellationToken))?.ToSession();

    public async Task<AccountSession> CreateFirstAdminAsync(NewAccount account, CancellationToken cancellationToken)
    {
        await validation.SetupIsOpenAsync(cancellationToken);
        var user = await BuildUserAsync(account, isAdmin: true, cancellationToken);
        await accounts.SaveUserAsync(user, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
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

        if (passwords.Verify(user.PasswordHash, request.Password ?? string.Empty))
        {
            if (security.NeedsUnlock(user))
            {
                security.Unlock(user);
                await accounts.SaveUserAsync(user, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
            }

            return user.ToAccountSession();
        }

        var message = security.RecordFailedSignIn(user, await settings.GetSecurityAsync(cancellationToken), now);
        await accounts.SaveUserAsync(user, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        throw new NotSignedInException(message);
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
        await unitOfWork.CommitAsync(cancellationToken);
        return user.ToAccountSession();
    }

    public async Task<AccountSession> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var link = await UsableLinkAsync(request.Token, LinkPurpose.PasswordReset, cancellationToken);
        var user = await validation.UserAsync(link.UserId ?? Guid.Empty, cancellationToken);

        security.SetPassword(user, passwords.Hash(ValidationEngine.Password(request.Password)));
        await accounts.SaveUserAsync(user, cancellationToken);
        await accounts.RemoveLinkAsync(link.Id, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
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
        await unitOfWork.CommitAsync(cancellationToken);
        return user.ToAccountSession();
    }

    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken) =>
        (await accounts.ListUsersAsync(cancellationToken)).OrderBy(user => user.CreatedAt).Select(user => user.ToResponse()).ToList();

    public async Task RemoveUserAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken)
    {
        ValidationEngine.CanRemove(id, actingUserId);
        var user = await validation.UserAsync(id, cancellationToken);
        await RemoveLinksAsync(link => link.UserId == user.Id, cancellationToken);
        await accounts.RemoveUserAsync(user.Id, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<UserResponse> UnlockUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await validation.UserAsync(id, cancellationToken);
        security.Unlock(user);
        await accounts.SaveUserAsync(user, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
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

            await unitOfWork.CommitAsync(cancellationToken);
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
        await unitOfWork.CommitAsync(cancellationToken);
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
        await unitOfWork.PublishAsync(new SecuritySettingsChanged(rules.RequireHttps));
        await unitOfWork.CommitAsync(cancellationToken);
        return rules.ToResponse();
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
        await unitOfWork.CommitAsync(cancellationToken);
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
