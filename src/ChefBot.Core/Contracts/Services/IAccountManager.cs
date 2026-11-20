using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.Contracts.Services;

/// <summary>First-run setup, signing in, passwords, lockout, inviting or removing people, and the sign-in rules.</summary>
public interface IAccountManager
{
    /// <summary>Whether setup is needed, who is signed in, and whether Mealie can be used.</summary>
    Task<SessionResponse> DescribeSessionAsync(Guid? userId, CancellationToken cancellationToken);

    /// <summary>The current state of an account, to check a sign-in cookie against. Null when it no longer exists.</summary>
    Task<SessionInfo?> FindSessionAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Creates the first account, as an admin. Refused once any account exists.</summary>
    Task<AccountSession> CreateFirstAdminAsync(NewAccount account, CancellationToken cancellationToken);

    /// <summary>
    /// Checks a username and password. Wrong passwords are counted, and too many in a row lock the account for a while.
    /// </summary>
    /// <exception cref="NotSignedInException">The details are wrong, or the account is locked.</exception>
    Task<AccountSession> SignInAsync(SignInRequest request, CancellationToken cancellationToken);

    /// <summary>What an opened link is for, so the page can show the right form.</summary>
    Task<LinkDetails> DescribeLinkAsync(string token, CancellationToken cancellationToken);

    /// <summary>Creates an account from an invite link and uses the invite up.</summary>
    Task<AccountSession> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken cancellationToken);

    /// <summary>Sets a new password from a reset link, uses the link up and unlocks the account.</summary>
    Task<AccountSession> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Changes the signed-in person's own password. Their other sessions are signed out.</summary>
    Task<AccountSession> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);

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
}
