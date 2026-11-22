using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <content>The planning settings (people, the weekly schedule and leftovers) and the calendar feed's meal times.</content>
internal sealed partial class PlanManager
{
    public async Task<PlannerSettingsResponse> GetPlannerSettingsAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetPlannerAsync(cancellationToken);
        return current.ToResponse(PlanningEngine.NextScheduledStaging(current, clock.GetLocalNow()));
    }

    public async Task<PlannerSettingsResponse> SavePlannerSettingsAsync(PlannerSettingsRequest request, CancellationToken cancellationToken)
    {
        var fresh = ValidationEngine.Planning(request);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var before = await settings.GetPlannerAsync(cancellationToken);
        await settings.SavePlannerAsync(fresh, cancellationToken);

        // Weeks still being reviewed follow the new count and leftovers; confirmed weeks are being shopped for as they are.
        if (fresh.People != before.People || fresh.MaxLeftoverDays != before.MaxLeftoverDays)
        {
            var stock = await SnapshotAsync(cancellationToken);
            var stores = await settings.ListStoresAsync(cancellationToken);
            foreach (var plan in await plans.ListOpenAsync(clock.Today(), cancellationToken))
            {
                if (plan.Status == PlanStatus.Staged)
                {
                    PlanningEngine.Resize(plan, fresh.People);
                    await ReconcileAsync(plan, stock, cancellationToken, stores);
                }
            }
        }

        await messages.CommitAsync(cancellationToken);
        fresh.IsSaved = true;
        return fresh.ToResponse(PlanningEngine.NextScheduledStaging(fresh, clock.GetLocalNow()));
    }

    public async Task<CalendarTimesResponse> GetCalendarTimesAsync(CancellationToken cancellationToken) =>
        (await settings.GetCalendarTimesAsync(cancellationToken)).ToResponse();

    public async Task<CalendarTimesResponse> SaveCalendarTimesAsync(CalendarTimesRequest request, CancellationToken cancellationToken)
    {
        var times = ValidationEngine.CalendarTimes(request);
        await settings.SaveCalendarTimesAsync(times, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        times.IsSaved = true;
        return times.ToResponse();
    }
}
