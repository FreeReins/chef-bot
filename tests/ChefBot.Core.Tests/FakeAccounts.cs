using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Tests;

internal sealed class FakeAccounts : IAccountAccess
{
    public List<UserAccount> Users { get; } = [];

    public List<AccessLink> Links { get; } = [];

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
}
