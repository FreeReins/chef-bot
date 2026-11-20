using ChefBot.Core.Accounts;

namespace ChefBot.Core.Abstractions;

/// <summary>Stored accounts.</summary>
public interface IUserRepository
{
    Task<bool> AnyAsync(CancellationToken cancellationToken);

    Task<User?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <param name="username">The normalized (lower-case) sign-in name.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<List<User>> ListAsync(CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}

/// <summary>Stored invite and password reset links.</summary>
public interface IAccessLinkRepository
{
    Task<AccessLink?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<AccessLink?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<List<AccessLink>> ListAsync(CancellationToken cancellationToken);

    void Add(AccessLink link);

    void Remove(AccessLink link);
}

/// <summary>
/// Settings an admin can change while ChefBot is running. Each kind of settings is one stored row.
/// </summary>
public interface ISettingsStore
{
    /// <summary>The current settings of a kind. Defaults are returned (and saved with the next commit) when none are stored yet.</summary>
    Task<TSettings> GetAsync<TSettings>(CancellationToken cancellationToken)
        where TSettings : class, new();
}

/// <summary>Hashes passwords so they can be checked without being stored.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}
