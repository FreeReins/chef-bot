using ChefBot.Core.Contracts.Messages;
using Wolverine;

namespace ChefBot.Api.Messaging;

/// <summary>
/// Shortly after startup and then every few minutes, asks for the jobs that run at a set time: the weekly plan,
/// the daily reminders and the daily backup. Each job checks for itself whether its time has come, so checking
/// often keeps them on time after a restart or a settings change, and an extra check never does anything twice.
/// </summary>
internal sealed class MaintenanceTimer(IServiceProvider services, ILogger<MaintenanceTimer> logger) : BackgroundService
{
    private static readonly TimeSpan FirstCheck = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstCheck, stoppingToken);
            using var timer = new PeriodicTimer(Interval);
            do
            {
                await CheckAsync();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
    }

    private async Task CheckAsync()
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new CheckWeeklySchedule());
            await bus.PublishAsync(new SendDueReminders());
            await bus.PublishAsync(new RunDueBackup());
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The scheduled jobs couldn't be queued. They will be tried again at the next check.");
        }
    }
}
