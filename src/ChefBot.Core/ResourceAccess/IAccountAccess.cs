using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

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

    Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken);

    Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken);

    Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken);

    Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken);
}
