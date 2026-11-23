using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Each person's stored inbox: the messages behind the bell on the web page.</summary>
internal interface IInboxAccess
{
    /// <summary>A person's messages, newest first, including any added earlier in this request.</summary>
    Task<List<InboxEntry>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(InboxEntry entry, CancellationToken cancellationToken);

    /// <summary>True when the person already has a message added by this message.</summary>
    Task<bool> HasSourceAsync(Guid userId, Guid sourceId, CancellationToken cancellationToken);

    /// <summary>Marks every unread message the person has as read at <paramref name="at"/>.</summary>
    Task MarkReadAsync(Guid userId, DateTimeOffset at, CancellationToken cancellationToken);

    /// <summary>Removes the given messages of a person's.</summary>
    Task RemoveAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Removes all of a person's messages.</summary>
    Task ClearAsync(Guid userId, CancellationToken cancellationToken);
}
