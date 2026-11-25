using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>
/// First-run setup, signing in, passwords, lockout, inviting or removing people, the sign-in rules, API tokens, and
/// each person's inbox.
/// </summary>
public interface IAccountManager
{
    /// <summary>Whether setup is needed and who is signed in.</summary>
    Task<SessionResponse> DescribeSessionAsync(Guid? userId, CancellationToken cancellationToken);

    /// <summary>The current state of an account, to check a sign-in cookie against. Null when it no longer exists.</summary>
    Task<SessionInfo?> FindSessionAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Creates the first account, as an admin. Refused once any account exists.</summary>
    Task<AccountSession> CreateFirstAdminAsync(NewAccount account, CancellationToken cancellationToken);

    /// <summary>
    /// Checks a username and password. Wrong passwords are counted, and too many in a row lock the account for a while.
    /// A device that signed in to the account before (<paramref name="knownDeviceUserId"/> is the account's id) isn't
    /// held up by the lockout and doesn't add to it, so someone guessing passwords can't lock the owner out.
    /// </summary>
    /// <param name="request">The username and password.</param>
    /// <param name="knownDeviceUserId">The account this device signed in to before, from its known-device cookie, or null.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    /// <exception cref="NotSignedInException">The details are wrong, or the account is locked.</exception>
    Task<AccountSession> SignInAsync(SignInRequest request, Guid? knownDeviceUserId, CancellationToken cancellationToken);

    /// <summary>What an opened link is for, so the page can show the right form.</summary>
    Task<LinkDetails> DescribeLinkAsync(string token, CancellationToken cancellationToken);

    /// <summary>Creates an account from an invite link and uses the invite up.</summary>
    Task<AccountSession> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Sets a new password from a reset link, uses the link up and unlocks the account. Its sessions end and its API
    /// tokens are revoked, since a reset is how a lost or stolen account is taken back.
    /// </summary>
    Task<AccountSession> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the signed-in person's own password. Their other sessions are signed out, and their API tokens are
    /// revoked when the request asks for that.
    /// </summary>
    Task<AccountSession> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session the person has, on every device. The session returned is a new one, for the device that
    /// asked. API tokens keep working.
    /// </summary>
    Task<AccountSession> SignOutEverywhereAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Every account, oldest first.</summary>
    Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken);

    /// <summary>Removes an account and its reset links. Admins can't remove themselves, so one always remains.</summary>
    Task RemoveUserAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken);

    /// <summary>Ends a lockout early.</summary>
    Task<UserResponse> UnlockUserAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A single-use password reset link for an account. Any earlier reset link for it stops working.</summary>
    Task<IssuedLink> CreatePasswordResetAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken);

    /// <summary>A reset link for a username, for the recovery setting at startup. Null when there is no such account.</summary>
    Task<IssuedLink?> CreateRecoveryLinkAsync(string username, CancellationToken cancellationToken);

    /// <summary>Creates a single-use invite.</summary>
    Task<IssuedLink> CreateInviteAsync(Guid actingUserId, CancellationToken cancellationToken);

    /// <summary>Links that can still be used, newest first. Expired ones are cleaned up along the way.</summary>
    Task<IReadOnlyList<PendingLink>> ListLinksAsync(CancellationToken cancellationToken);

    /// <summary>Replaces an unused link with a new one for the same purpose. The old link stops working.</summary>
    Task<IssuedLink> RegenerateLinkAsync(Guid id, Guid actingUserId, CancellationToken cancellationToken);

    Task RevokeLinkAsync(Guid id, CancellationToken cancellationToken);

    Task<SecurityResponse> GetSecurityAsync(CancellationToken cancellationToken);

    /// <summary>Changes the lockout rules and whether plain HTTP is redirected to HTTPS. Publishes <see cref="SecuritySettingsChanged"/>.</summary>
    Task<SecurityResponse> UpdateSecurityAsync(SecurityRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Turns "always use HTTPS" on when ChefBot serves HTTPS itself and no admin has saved the sign-in rules yet, so
    /// passwords aren't sent over plain HTTP by default. Rules an admin saved are left alone. Returns the rules now.
    /// </summary>
    Task<SecurityResponse> UseDefaultHttpsRuleAsync(bool servesHttps, CancellationToken cancellationToken);

    /// <summary>The API tokens that sign in as an account, newest first. Their secrets can't be shown again.</summary>
    Task<IReadOnlyList<ApiTokenResponse>> ListApiTokensAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an API token that signs in as an account, for an assistant such as an MCP client. It keeps working
    /// until it expires, is revoked, its owner resets their password or the account is removed. A read-only token can
    /// only use tools that change nothing. The secret is returned only here.
    /// </summary>
    /// <exception cref="RuleViolationException">The name is blank or too long, or the expiry is out of range.</exception>
    Task<IssuedApiToken> CreateApiTokenAsync(Guid userId, CreateApiTokenRequest request, CancellationToken cancellationToken);

    /// <summary>Stops one of an account's API tokens from working.</summary>
    /// <exception cref="NotFoundException">The account has no token with that id.</exception>
    Task RevokeApiTokenAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The account an API token signs in as, as it is now, and whether the token is read-only. Records that the token
    /// was used. Null when the token has expired or it or its account no longer exists.
    /// </summary>
    Task<ApiTokenSession?> FindApiTokenSessionAsync(string token, CancellationToken cancellationToken);

    /// <summary>A person's inbox: the newest messages, newest first, and how many are unread.</summary>
    Task<InboxResponse> GetInboxAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a note from the web page to a person's own inbox, unread. Only the newest messages are kept. Publishes
    /// <see cref="InboxChanged"/>.
    /// </summary>
    /// <exception cref="RuleViolationException">The text is blank or too long.</exception>
    Task<InboxResponse> AddInboxNoteAsync(Guid userId, InboxNoteRequest request, CancellationToken cancellationToken);

    /// <summary>Marks everything in a person's inbox read. Publishes <see cref="InboxChanged"/>.</summary>
    Task<InboxResponse> MarkInboxReadAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Empties a person's inbox. Publishes <see cref="InboxChanged"/>.</summary>
    Task<InboxResponse> ClearInboxAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Tells everyone in their inbox that the weekly schedule staged a plan. Plans staged by a person are left out:
    /// the page told them already. Handling the same message twice adds nothing.
    /// </summary>
    Task AnnouncePlanStagedAsync(PlanStaged message, Guid messageId, CancellationToken cancellationToken);
}
