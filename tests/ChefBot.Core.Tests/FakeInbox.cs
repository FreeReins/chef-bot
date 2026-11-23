using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeInbox : IInboxAccess
{
    public List<InboxEntry> Rows { get; } = [];

    public Task<List<InboxEntry>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.CreatedAt).ThenByDescending(entry => entry.Id).ToList());

    public Task AddAsync(InboxEntry entry, CancellationToken cancellationToken)
    {
        Rows.Add(entry);
        return Task.CompletedTask;
    }

    public Task<bool> HasSourceAsync(Guid userId, Guid sourceId, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Any(entry => entry.UserId == userId && entry.SourceId == sourceId));

    public Task MarkReadAsync(Guid userId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        foreach (var entry in Rows.Where(entry => entry.UserId == userId && entry.ReadAt is null))
        {
            entry.ReadAt = at;
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(entry => entry.UserId == userId && ids.Contains(entry.Id));
        return Task.CompletedTask;
    }

    public Task ClearAsync(Guid userId, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(entry => entry.UserId == userId);
        return Task.CompletedTask;
    }
}
