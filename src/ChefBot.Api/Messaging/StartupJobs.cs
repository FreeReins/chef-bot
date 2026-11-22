using ChefBot.Core.Managers.Contracts.Messages;
using Wolverine;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Once the app has started, asks each scheduled job to look now and schedule its next run: the weekly plan, the
/// daily reminders and the daily backup. From then on each job schedules its own next run in the database, so they
/// survive restarts; a run missed while ChefBot was off happens straight away. It also records the achievements
/// reached so far, so only new ones are announced.
/// </summary>
internal sealed class StartupJobs(IServiceProvider services, IHostApplicationLifetime lifetime, ILogger<StartupJobs> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStarted.Register(() => _ = QueueAsync());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task QueueAsync()
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new CheckWeeklySchedule());
            await bus.PublishAsync(new SendDueReminders());
            await bus.PublishAsync(new RunDueBackup());
            await bus.PublishAsync(new CheckAchievements());
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The scheduled jobs couldn't be started. Restart ChefBot to try again.");
        }
    }
}
