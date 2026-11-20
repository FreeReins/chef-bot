namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// One transaction: every database change made through the accessors, and every message published, is saved
/// together by <see cref="CommitAsync"/>. Messages are only sent once the data they describe is saved.
/// </summary>
internal interface IUnitOfWork
{
    /// <summary>Queues a command or event, sent when the transaction commits.</summary>
    Task PublishAsync(object message);

    Task CommitAsync(CancellationToken cancellationToken);
}
