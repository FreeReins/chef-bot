using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
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

    public async Task<SignInAttempt> CountSignInAttemptAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Only an unlocked account is counted. A LockedUntil still set here has run out, so counting starts again.
        var counted = await db.Users
            .Where(user => user.Id == userId && (user.LockedUntil == null || user.LockedUntil <= now))
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(user => user.FailedSignIns, user => user.LockedUntil == null ? user.FailedSignIns + 1 : 1)
                    .SetProperty(user => user.LockedUntil, (DateTimeOffset?)null),
                cancellationToken);

        // Read back separately. The count only goes up until a sign-in succeeds, so this is never lower than this
        // attempt's own number, and an attempt that reads a number past the limit really is past it.
        var state = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.FailedSignIns, user.LockedUntil })
            .FirstOrDefaultAsync(cancellationToken);

        return counted == 1 && state is not null
            ? new SignInAttempt(state.FailedSignIns, null)
            : new SignInAttempt(null, state?.LockedUntil);
    }

    public Task ClearSignInAttemptsAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.Where(user => user.Id == userId).ExecuteUpdateAsync(
            set => set.SetProperty(user => user.FailedSignIns, 0).SetProperty(user => user.LockedUntil, (DateTimeOffset?)null),
            cancellationToken);

    public Task LockAsync(Guid userId, DateTimeOffset until, CancellationToken cancellationToken) =>
        db.Users.Where(user => user.Id == userId).ExecuteUpdateAsync(
            set => set.SetProperty(user => user.LockedUntil, until),
            cancellationToken);

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

    public async Task<ApiToken?> FindApiTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        (db.ApiTokens.Local.FirstOrDefault(token => token.TokenHash == tokenHash)
            ?? await db.ApiTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken))?.ToContract();

    public async Task<List<ApiToken>> ListApiTokensAsync(Guid userId, CancellationToken cancellationToken) =>
        (await db.ApiTokens.Where(token => token.UserId == userId).ToListAsync(cancellationToken)).Select(token => token.ToContract()).ToList();

    public async Task SaveApiTokenAsync(ApiToken token, CancellationToken cancellationToken)
    {
        var fresh = token.ToEntity();
        if (await db.ApiTokens.FindAsync([token.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.ApiTokens.Add(fresh);
        }
    }

    public async Task RemoveApiTokenAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.ApiTokens.FindAsync([id], cancellationToken) is { } token)
        {
            db.ApiTokens.Remove(token);
        }
    }
}
