using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Stored accounts and their invite and password reset links.</summary>
internal interface IAccountAccess
{
    Task<bool> AnyUsersAsync(CancellationToken cancellationToken);

    Task<UserAccount?> FindUserAsync(Guid id, CancellationToken cancellationToken);

    /// <param name="username">The normalized (lower-case) sign-in name.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    Task<UserAccount?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<List<UserAccount>> ListUsersAsync(CancellationToken cancellationToken);

    Task SaveUserAsync(UserAccount user, CancellationToken cancellationToken);

    Task RemoveUserAsync(Guid id, CancellationToken cancellationToken);

    // The three sign-in counters below change the database straight away, in one statement each, instead of at the
    // next commit. Sign-ins tried at the same moment then each see the others' attempts, so they can't check more
    // passwords between them than the lockout rules allow.

    /// <summary>
    /// Counts a sign-in attempt for an account that isn't locked, before its password is checked. A lockout that has
    /// run out is cleared, and counting starts again from one.
    /// </summary>
    Task<SignInAttempt> CountSignInAttemptAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Forgets the counted attempts and any lockout, after a successful sign-in.</summary>
    Task ClearSignInAttemptsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Refuses sign-in to the account until <paramref name="until"/>.</summary>
    Task LockAsync(Guid userId, DateTimeOffset until, CancellationToken cancellationToken);

    Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken);

    Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken);

    Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken);

    Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken);
}
