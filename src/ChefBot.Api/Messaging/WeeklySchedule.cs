using ChefBot.Core.Planning;
using Wolverine;

namespace ChefBot.Api.Messaging;

public static class WeeklySchedule
{
    /// <summary>
    /// Books the first <see cref="WeeklyPlanDue"/> and <see cref="MaintenanceTick"/> messages after startup.
    /// Each run books the next one, so the schedules are held in memory and start again when the app restarts.
    /// </summary>
    public static async Task ScheduleNextWeeklyPlanAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Daily reminders and backups are checked shortly after startup, then every few minutes.
        await bus.ScheduleAsync(new MaintenanceTick(), TimeSpan.FromMinutes(1));

        var schedule = services.GetRequiredService<WeeklyScheduleOptions>();
        if (!schedule.Enabled)
        {
            return;
        }

        var next = schedule.NextOccurrence(services.GetRequiredService<TimeProvider>().GetLocalNow());
        await bus.ScheduleAsync(new WeeklyPlanDue(), next);

        services.GetRequiredService<ILoggerFactory>().CreateLogger("ChefBot.Schedule")
            .LogInformation("Next week's plan will be staged automatically at {When}.", next);
    }
}
