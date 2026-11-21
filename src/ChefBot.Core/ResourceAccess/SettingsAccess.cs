using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <inheritdoc cref="ISettingsAccess"/>
internal sealed class SettingsAccess(ChefBotDbContext db) : ISettingsAccess
{
    public async Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) =>
        await RowAsync<SecuritySettingsEntity>(cancellationToken) is { } row
            ? new SecuritySettings { MaxFailedSignIns = row.MaxFailedSignIns, LockoutMinutes = row.LockoutMinutes, RequireHttps = row.RequireHttps }
            : new SecuritySettings();

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new SecuritySettingsEntity
        {
            MaxFailedSignIns = settings.MaxFailedSignIns,
            LockoutMinutes = settings.LockoutMinutes,
            RequireHttps = settings.RequireHttps,
        }, cancellationToken);

    public async Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) =>
        await RowAsync<NotificationSettingsEntity>(cancellationToken) is { } row
            ? new NotificationSettings
            {
                WebhookUrl = row.WebhookUrl,
                PlanReady = row.PlanReady,
                SlowCooker = row.SlowCooker,
                MealsToday = row.MealsToday,
                ExpiringFood = row.ExpiringFood,
                Shopping = row.Shopping,
                DailyTime = row.DailyTime,
                LastDailyRunOn = row.LastDailyRunOn,
            }
            : new NotificationSettings();

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new NotificationSettingsEntity
        {
            WebhookUrl = settings.WebhookUrl,
            PlanReady = settings.PlanReady,
            SlowCooker = settings.SlowCooker,
            MealsToday = settings.MealsToday,
            ExpiringFood = settings.ExpiringFood,
            Shopping = settings.Shopping,
            DailyTime = settings.DailyTime,
            LastDailyRunOn = settings.LastDailyRunOn,
        }, cancellationToken);

    public async Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) =>
        await RowAsync<BackupSettingsEntity>(cancellationToken) is { } row
            ? new BackupSettings
            {
                Enabled = row.Enabled,
                Time = row.Time,
                KeepCount = row.KeepCount,
                KeepDays = row.KeepDays,
                LastRunOn = row.LastRunOn,
                LastResult = row.LastResult,
            }
            : new BackupSettings();

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new BackupSettingsEntity
        {
            Enabled = settings.Enabled,
            Time = settings.Time,
            KeepCount = settings.KeepCount,
            KeepDays = settings.KeepDays,
            LastRunOn = settings.LastRunOn,
            LastResult = settings.LastResult,
        }, cancellationToken);

    public async Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken) =>
        await RowAsync<ScheduleStateEntity>(cancellationToken) is { } row
            ? new ScheduleState { LastStagedWeek = row.LastStagedWeek }
            : new ScheduleState();

    public Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken) =>
        SaveRowAsync(new ScheduleStateEntity { LastStagedWeek = state.LastStagedWeek }, cancellationToken);

    public async Task<List<GroceryStore>> ListStoresAsync(CancellationToken cancellationToken) =>
        (await db.GroceryStores.ToListAsync(cancellationToken))
            .OrderBy(store => store.Position)
            .Select(store => store.ToContract())
            .ToList();

    public async Task SaveStoresAsync(IReadOnlyList<GroceryStore> stores, CancellationToken cancellationToken)
    {
        var tracked = await db.GroceryStores.ToDictionaryAsync(store => store.Id, cancellationToken);
        var wanted = stores.Select(store => store.ToEntity()).ToList();
        var wantedIds = wanted.Select(store => store.Id).ToHashSet();

        db.GroceryStores.RemoveRange(tracked.Values.Where(store => !wantedIds.Contains(store.Id)));
        foreach (var fresh in wanted)
        {
            if (tracked.TryGetValue(fresh.Id, out var row))
            {
                db.CopyInto(row, fresh);
            }
            else
            {
                db.GroceryStores.Add(fresh);
            }
        }
    }

    public async Task<List<PlanStyle>> ListPlanStylesAsync(CancellationToken cancellationToken) =>
        (await db.PlanStyles.ToListAsync(cancellationToken))
            .OrderBy(style => style.Position)
            .Select(style => style.ToContract())
            .ToList();

    public async Task SavePlanStylesAsync(IReadOnlyList<PlanStyle> styles, CancellationToken cancellationToken)
    {
        var tracked = await db.PlanStyles.ToDictionaryAsync(style => style.Id, cancellationToken);
        var wanted = styles.Select(style => style.ToEntity()).ToList();
        var wantedIds = wanted.Select(style => style.Id).ToHashSet();

        db.PlanStyles.RemoveRange(tracked.Values.Where(style => !wantedIds.Contains(style.Id)));
        foreach (var fresh in wanted)
        {
            if (tracked.TryGetValue(fresh.Id, out var row))
            {
                db.CopyInto(row, fresh);
            }
            else
            {
                db.PlanStyles.Add(fresh);
            }
        }
    }

    /// <summary>The single row of a settings table, if one has been saved (or added earlier in this request).</summary>
    /// <remarks>Every settings row has Id 1; looking it up by key also finds a row added but not yet saved.</remarks>
    private async Task<TRow?> RowAsync<TRow>(CancellationToken cancellationToken)
        where TRow : class =>
        await db.Set<TRow>().FindAsync([1], cancellationToken);

    private async Task SaveRowAsync<TRow>(TRow fresh, CancellationToken cancellationToken)
        where TRow : class
    {
        if (await RowAsync<TRow>(cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Set<TRow>().Add(fresh);
        }
    }
}
