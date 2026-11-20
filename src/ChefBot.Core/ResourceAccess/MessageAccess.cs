using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Wolverine.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Saves the database changes and sends the queued messages together, through Wolverine's EF Core outbox: the
/// messages are written to the database in the same transaction and sent once it commits.
/// </summary>
internal sealed class MessageAccess(IDbContextOutbox<ChefBotDbContext> outbox) : IMessageAccess
{
    public Task PublishAsync(object message) => outbox.PublishAsync(message).AsTask();

    public Task CommitAsync(CancellationToken cancellationToken) => outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
}
