using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>
/// The weekly schedule: one scheduled <see cref="CheckWeeklySchedule"/> at a time, due when the schedule next fires.
/// The one saved as due (<c>ScheduleState.WeeklyDueAt</c>) is the only one acted on, so scheduling afresh, after a
/// settings change or a restart, leaves any older one to do nothing.
/// </content>
internal sealed partial class PlanManager
{
    public async Task CheckWeeklyScheduleAsync(CheckWeeklySchedule command, CancellationToken cancellationToken)
    {
        var state = await settings.GetScheduleAsync(cancellationToken);
        if (command.DueAt is not null && command.DueAt != state.WeeklyDueAt)
        {
            return;
        }

        try
        {
            await StageScheduledWeekAsync(cancellationToken);
        }
        finally
        {
            // Even when the week couldn't be planned (say, no recipes yet), so next week gets its try.
            await ScheduleWeeklyCheckAsync(cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }
    }

    /// <summary>Stages the week the schedule is due for, if it is due and that week has no plan yet.</summary>
    private async Task StageScheduledWeekAsync(CancellationToken cancellationToken)
    {
        var state = await settings.GetScheduleAsync(cancellationToken);
        var schedule = await settings.GetPlannerAsync(cancellationToken);
        if (PlanningEngine.ScheduledWeekDue(schedule, clock.GetLocalNow(), state.LastStagedWeek) is not DateOnly week)
        {
            return;
        }

        // Recorded first, so a week that can't be planned (say, no recipes yet) isn't tried again and again.
        state.LastStagedWeek = week;
        await settings.SaveScheduleAsync(state, cancellationToken);
        await messages.CommitAsync(cancellationToken);

        if (await plans.FindByWeekAsync(week, cancellationToken) is null)
        {
            var style = await StyleAsync(id: null, cancellationToken);
            await StageAsync(week, replace: false, favorPantry: false, "schedule", style, [], cancellationToken);
        }
    }

    /// <summary>
    /// Schedules the next check for when the schedule next fires, replacing any check scheduled before; none while
    /// the schedule is off. Saved with the caller's commit.
    /// </summary>
    private async Task ScheduleWeeklyCheckAsync(CancellationToken cancellationToken)
    {
        var state = await settings.GetScheduleAsync(cancellationToken);
        var next = PlanningEngine.NextScheduledStaging(await settings.GetPlannerAsync(cancellationToken), clock.GetLocalNow());
        state.WeeklyDueAt = next is { } at ? DailyTime.Seconds(at.ToUniversalTime()) : null;
        await settings.SaveScheduleAsync(state, cancellationToken);
        if (state.WeeklyDueAt is { } due)
        {
            await messages.ScheduleAsync(new CheckWeeklySchedule(due), due);
        }
    }
}
