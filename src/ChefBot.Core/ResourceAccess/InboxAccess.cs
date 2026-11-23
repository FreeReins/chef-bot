using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="IInboxAccess"/>
internal sealed class InboxAccess(ChefBotDbContext db) : IInboxAccess
{
    public async Task<List<InboxEntry>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        (await TrackedAsync(userId, cancellationToken))
        .OrderByDescending(entry => entry.CreatedAt)
        .ThenByDescending(entry => entry.Id)
        .Select(entry => entry.ToContract())
        .ToList();

    public Task AddAsync(InboxEntry entry, CancellationToken cancellationToken)
    {
        db.InboxEntries.Add(entry.ToEntity());
        return Task.CompletedTask;
    }

    public async Task<bool> HasSourceAsync(Guid userId, Guid sourceId, CancellationToken cancellationToken) =>
        db.InboxEntries.Local.Any(entry => entry.UserId == userId && entry.SourceId == sourceId)
        || await db.InboxEntries.AnyAsync(entry => entry.UserId == userId && entry.SourceId == sourceId, cancellationToken);

    public async Task MarkReadAsync(Guid userId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        foreach (var entry in (await TrackedAsync(userId, cancellationToken)).Where(entry => entry.ReadAt is null))
        {
            entry.ReadAt = at;
        }
    }

    public async Task RemoveAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        db.InboxEntries.RemoveRange((await TrackedAsync(userId, cancellationToken)).Where(entry => ids.Contains(entry.Id)).ToList());

    public async Task ClearAsync(Guid userId, CancellationToken cancellationToken) =>
        db.InboxEntries.RemoveRange(await TrackedAsync(userId, cancellationToken));

    /// <summary>Loading them tracks them, so changes are saved on commit and one added earlier in this request is found too.</summary>
    private async Task<List<InboxEntryEntity>> TrackedAsync(Guid userId, CancellationToken cancellationToken)
    {
        await db.InboxEntries.Where(entry => entry.UserId == userId).LoadAsync(cancellationToken);
        return db.InboxEntries.Local.Where(entry => entry.UserId == userId).ToList();
    }
}
