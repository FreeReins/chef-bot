using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeAccounts : IAccountAccess
{
    public List<UserAccount> Users { get; } = [];

    public List<AccessLink> Links { get; } = [];

    public List<ApiToken> ApiTokens { get; } = [];

    public UserAccount User(string username) => Users.Single(u => u.Username == username);

    public Task<bool> AnyUsersAsync(CancellationToken cancellationToken) => Task.FromResult(Users.Count > 0);

    public Task<UserAccount?> FindUserAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id) is { } row ? Copies.Of(row) : null);

    public Task<UserAccount?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Username == username) is { } row ? Copies.Of(row) : null);

    public Task<List<UserAccount>> ListUsersAsync(CancellationToken cancellationToken) => Task.FromResult(Users.Select(Copies.Of).ToList());

    public Task SaveUserAsync(UserAccount user, CancellationToken cancellationToken)
    {
        Users.RemoveAll(u => u.Id == user.Id);
        Users.Add(Copies.Of(user));
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        Users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }

    public Task<SignInAttempt> CountSignInAttemptAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var user = Users.FirstOrDefault(u => u.Id == userId);
        if (user is null || user.LockedUntil > now)
        {
            return Task.FromResult(new SignInAttempt(null, user?.LockedUntil));
        }

        user.FailedSignIns = user.LockedUntil is null ? user.FailedSignIns + 1 : 1;
        user.LockedUntil = null;
        return Task.FromResult(new SignInAttempt(user.FailedSignIns, null));
    }

    public Task ClearSignInAttemptsAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var user in Users.Where(u => u.Id == userId))
        {
            user.FailedSignIns = 0;
            user.LockedUntil = null;
        }

        return Task.CompletedTask;
    }

    public Task LockAsync(Guid userId, DateTimeOffset until, CancellationToken cancellationToken)
    {
        foreach (var user in Users.Where(u => u.Id == userId))
        {
            user.LockedUntil = until;
        }

        return Task.CompletedTask;
    }

    public Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Links.FirstOrDefault(l => l.Id == id) is { } row ? Copies.Of(row) : null);

    public Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Links.FirstOrDefault(l => l.TokenHash == tokenHash) is { } row ? Copies.Of(row) : null);

    public Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken) => Task.FromResult(Links.Select(Copies.Of).ToList());

    public Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken)
    {
        Links.RemoveAll(l => l.Id == link.Id);
        Links.Add(Copies.Of(link));
        return Task.CompletedTask;
    }

    public Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        Links.RemoveAll(l => l.Id == id);
        return Task.CompletedTask;
    }

    public Task<ApiToken?> FindApiTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(ApiTokens.FirstOrDefault(t => t.TokenHash == tokenHash) is { } row ? Copies.Of(row) : null);

    public Task<List<ApiToken>> ListApiTokensAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(ApiTokens.Where(t => t.UserId == userId).Select(Copies.Of).ToList());

    public Task SaveApiTokenAsync(ApiToken token, CancellationToken cancellationToken)
    {
        ApiTokens.RemoveAll(t => t.Id == token.Id);
        ApiTokens.Add(Copies.Of(token));
        return Task.CompletedTask;
    }

    public Task RemoveApiTokenAsync(Guid id, CancellationToken cancellationToken)
    {
        ApiTokens.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }

    public Task MarkApiTokenUsedAsync(Guid id, DateTimeOffset now, DateTimeOffset since, CancellationToken cancellationToken)
    {
        if (ApiTokens.FirstOrDefault(t => t.Id == id) is { } token && (token.LastUsedAt is null || token.LastUsedAt < since))
        {
            token.LastUsedAt = now;
        }

        return Task.CompletedTask;
    }
}
