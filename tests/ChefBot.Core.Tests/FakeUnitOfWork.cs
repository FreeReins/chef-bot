using ChefBot.Core.ResourceAccess;

namespace ChefBot.Core.Tests;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<object> Published { get; } = [];

    public int Commits { get; private set; }

    public Task PublishAsync(object message)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Commits++;
        return Task.CompletedTask;
    }
}
