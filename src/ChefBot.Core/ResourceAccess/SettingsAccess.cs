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
            ? new SecuritySettings
            {
                MaxFailedSignIns = row.MaxFailedSignIns,
                LockoutMinutes = row.LockoutMinutes,
                RequireHttps = row.RequireHttps,
                IsSaved = true,
            }
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
                Achievements = row.Achievements,
                RemindersDueAt = row.RemindersDueAt,
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
            Achievements = settings.Achievements,
            RemindersDueAt = settings.RemindersDueAt,
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
                DueAt = row.DueAt,
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
            DueAt = settings.DueAt,
        }, cancellationToken);

    public async Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken) =>
        await RowAsync<ScheduleStateEntity>(cancellationToken) is { } row
            ? new ScheduleState { LastStagedWeek = row.LastStagedWeek, WeeklyDueAt = row.WeeklyDueAt }
            : new ScheduleState();

    public Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken) =>
        SaveRowAsync(new ScheduleStateEntity { LastStagedWeek = state.LastStagedWeek, WeeklyDueAt = state.WeeklyDueAt }, cancellationToken);

    public async Task<CalendarFeed> GetCalendarFeedAsync(CancellationToken cancellationToken) =>
        await RowAsync<CalendarFeedEntity>(cancellationToken) is { } row
            ? new CalendarFeed { Token = row.Token, CreatedAt = row.CreatedAt }
            : new CalendarFeed();

    public Task SaveCalendarFeedAsync(CalendarFeed feed, CancellationToken cancellationToken) =>
        SaveRowAsync(new CalendarFeedEntity { Token = feed.Token, CreatedAt = feed.CreatedAt }, cancellationToken);

    public async Task<PlannerSettings> GetPlannerAsync(CancellationToken cancellationToken) =>
        await RowAsync<PlannerSettingsEntity>(cancellationToken) is { } row
            ? new PlannerSettings
            {
                People = row.People,
                ScheduleEnabled = row.ScheduleEnabled,
                ScheduleDay = row.ScheduleDay,
                ScheduleTime = row.ScheduleTime,
                MaxLeftoverDays = row.MaxLeftoverDays,
                IsSaved = true,
            }
            : new PlannerSettings();

    public Task SavePlannerAsync(PlannerSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new PlannerSettingsEntity
        {
            People = settings.People,
            ScheduleEnabled = settings.ScheduleEnabled,
            ScheduleDay = settings.ScheduleDay,
            ScheduleTime = settings.ScheduleTime,
            MaxLeftoverDays = settings.MaxLeftoverDays,
        }, cancellationToken);

    public async Task<AdvancedSettings> GetAdvancedAsync(CancellationToken cancellationToken) =>
        await RowAsync<AdvancedSettingsEntity>(cancellationToken) is { } row
            ? new AdvancedSettings
            {
                AvoidRepeatDays = row.AvoidRepeatDays,
                ExpiringSoonDays = row.ExpiringSoonDays,
                PerishableWithinDays = row.PerishableWithinDays,
                Words = new ClassifierWords
                {
                    BreakfastWords = [.. row.BreakfastWords],
                    SnackWords = [.. row.SnackWords],
                    SweetWords = [.. row.SweetWords],
                    DrinkWords = [.. row.DrinkWords],
                    SlowCookerWords = [.. row.SlowCookerWords],
                    NotAMealWords = [.. row.NotAMealWords],
                    MainWords = [.. row.MainWords],
                },
                IsSaved = true,
            }
            : new AdvancedSettings();

    public Task SaveAdvancedAsync(AdvancedSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new AdvancedSettingsEntity
        {
            AvoidRepeatDays = settings.AvoidRepeatDays,
            ExpiringSoonDays = settings.ExpiringSoonDays,
            PerishableWithinDays = settings.PerishableWithinDays,
            BreakfastWords = [.. settings.Words.BreakfastWords],
            SnackWords = [.. settings.Words.SnackWords],
            SweetWords = [.. settings.Words.SweetWords],
            DrinkWords = [.. settings.Words.DrinkWords],
            SlowCookerWords = [.. settings.Words.SlowCookerWords],
            NotAMealWords = [.. settings.Words.NotAMealWords],
            MainWords = [.. settings.Words.MainWords],
        }, cancellationToken);

    public async Task<CalendarSettings> GetCalendarTimesAsync(CancellationToken cancellationToken) =>
        await RowAsync<CalendarSettingsEntity>(cancellationToken) is { } row
            ? new CalendarSettings
            {
                BreakfastTime = row.BreakfastTime,
                LunchTime = row.LunchTime,
                DinnerTime = row.DinnerTime,
                MealMinutes = row.MealMinutes,
                SlowCookerHours = row.SlowCookerHours,
                IsSaved = true,
            }
            : new CalendarSettings();

    public Task SaveCalendarTimesAsync(CalendarSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new CalendarSettingsEntity
        {
            BreakfastTime = settings.BreakfastTime,
            LunchTime = settings.LunchTime,
            DinnerTime = settings.DinnerTime,
            MealMinutes = settings.MealMinutes,
            SlowCookerHours = settings.SlowCookerHours,
        }, cancellationToken);

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

    public async Task<List<ShoppingTarget>> ListShoppingTargetsAsync(CancellationToken cancellationToken) =>
        (await db.ShoppingTargets.ToListAsync(cancellationToken))
            .OrderBy(target => target.Position)
            .Select(target => target.ToContract())
            .ToList();

    public async Task SaveShoppingTargetsAsync(IReadOnlyList<ShoppingTarget> targets, CancellationToken cancellationToken)
    {
        var tracked = await db.ShoppingTargets.ToDictionaryAsync(target => target.Id, cancellationToken);
        var wanted = targets.Select(target => target.ToEntity()).ToList();
        var wantedIds = wanted.Select(target => target.Id).ToHashSet();

        db.ShoppingTargets.RemoveRange(tracked.Values.Where(target => !wantedIds.Contains(target.Id)));
        foreach (var fresh in wanted)
        {
            if (tracked.TryGetValue(fresh.Id, out var row))
            {
                db.CopyInto(row, fresh);
            }
            else
            {
                db.ShoppingTargets.Add(fresh);
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

    public async Task<AiSettings> GetAiAsync(CancellationToken cancellationToken) => new()
    {
        Models = (await db.AiModels.ToListAsync(cancellationToken))
            .OrderBy(model => model.Position)
            .Select(model => model.ToContract())
            .ToList(),
        Routes = (await db.AiRoutes.ToListAsync(cancellationToken)).Select(route => route.ToContract()).ToList(),
    };

    public async Task SaveAiAsync(AiSettings ai, CancellationToken cancellationToken)
    {
        var tracked = await db.AiModels.ToDictionaryAsync(model => model.Id, cancellationToken);
        var wanted = ai.Models.Select(model => model.ToEntity()).ToList();
        var wantedIds = wanted.Select(model => model.Id).ToHashSet();

        db.AiModels.RemoveRange(tracked.Values.Where(model => !wantedIds.Contains(model.Id)));
        foreach (var fresh in wanted)
        {
            if (tracked.TryGetValue(fresh.Id, out var row))
            {
                db.CopyInto(row, fresh);
            }
            else
            {
                db.AiModels.Add(fresh);
            }
        }

        var trackedRoutes = await db.AiRoutes.ToDictionaryAsync(route => route.Task, cancellationToken);
        var wantedRoutes = ai.Routes.Select(route => route.ToEntity()).ToList();
        var wantedTasks = wantedRoutes.Select(route => route.Task).ToHashSet();
        db.AiRoutes.RemoveRange(trackedRoutes.Values.Where(route => !wantedTasks.Contains(route.Task)));
        foreach (var fresh in wantedRoutes)
        {
            if (trackedRoutes.TryGetValue(fresh.Task, out var row))
            {
                row.ModelId = fresh.ModelId;
            }
            else
            {
                db.AiRoutes.Add(fresh);
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
