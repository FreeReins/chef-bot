using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeMessages : IMessageAccess
{
    public List<object> Published { get; } = [];

    /// <summary>Commands scheduled for later, with when they are due.</summary>
    public List<(object Message, DateTimeOffset At)> Scheduled { get; } = [];

    public int Commits { get; private set; }

    public Task PublishAsync(object message)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task ScheduleAsync(object message, DateTimeOffset at)
    {
        Scheduled.Add((message, at));
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Commits++;
        return Task.CompletedTask;
    }
}
