using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IAccountAccess"/>
internal sealed class AccountAccess(ChefBotDbContext db) : IAccountAccess
{
    public async Task<bool> AnyUsersAsync(CancellationToken cancellationToken) =>
        db.Users.Local.Count > 0 || await db.Users.AnyAsync(cancellationToken);

    public async Task<UserAccount?> FindUserAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.Users.FindAsync([id], cancellationToken))?.ToContract();

    public async Task<UserAccount?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken) =>
        (db.Users.Local.FirstOrDefault(user => user.Username == username)
            ?? await db.Users.FirstOrDefaultAsync(user => user.Username == username, cancellationToken))?.ToContract();

    public async Task<List<UserAccount>> ListUsersAsync(CancellationToken cancellationToken) =>
        (await db.Users.ToListAsync(cancellationToken)).Select(user => user.ToContract()).ToList();

    public async Task SaveUserAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var fresh = user.ToEntity();
        if (await db.Users.FindAsync([user.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Users.Add(fresh);
        }
    }

    public async Task RemoveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.Users.FindAsync([id], cancellationToken) is { } user)
        {
            db.Users.Remove(user);
        }
    }

    public async Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.AccessLinks.FindAsync([id], cancellationToken))?.ToContract();

    public async Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        (await db.AccessLinks.FirstOrDefaultAsync(link => link.TokenHash == tokenHash, cancellationToken))?.ToContract();

    public async Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken) =>
        (await db.AccessLinks.ToListAsync(cancellationToken)).Select(link => link.ToContract()).ToList();

    public async Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken)
    {
        var fresh = link.ToEntity();
        if (await db.AccessLinks.FindAsync([link.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.AccessLinks.Add(fresh);
        }
    }

    public async Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.AccessLinks.FindAsync([id], cancellationToken) is { } link)
        {
            db.AccessLinks.Remove(link);
        }
    }
}
