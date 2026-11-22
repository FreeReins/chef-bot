namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>
/// The message bus, through Wolverine's outbox. It also owns the transaction: every database change made through
/// the other accessors, and every message published, is saved together by <see cref="CommitAsync"/>. Messages are
/// only sent once the data they describe is saved.
/// </summary>
internal interface IMessageAccess
{
    /// <summary>Queues a command or event, sent when the transaction commits.</summary>
    Task PublishAsync(object message);

    /// <summary>
    /// Queues a command to be handled at <paramref name="at"/>, kept in the database until then, so it survives a
    /// restart. Saved by <see cref="CommitAsync"/> like everything else.
    /// </summary>
    Task ScheduleAsync(object message, DateTimeOffset at);

    /// <summary>Saves the database changes and sends the queued messages.</summary>
    Task CommitAsync(CancellationToken cancellationToken);
}
