using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>Settings an admin can change while ChefBot is running. Defaults are returned when none are saved yet.</summary>
internal interface ISettingsAccess
{
    Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken);

    Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken);

    Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken);

    Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken);

    Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken);

    Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken);

    Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken);

    Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken);

    /// <summary>The calendar feed; one with an empty token when it has never been turned on.</summary>
    Task<CalendarFeed> GetCalendarFeedAsync(CancellationToken cancellationToken);

    Task SaveCalendarFeedAsync(CalendarFeed feed, CancellationToken cancellationToken);

    /// <summary>The household's stores in their saved order. Empty when none are set up.</summary>
    /// <summary>The people plans are for, the weekly schedule and how long leftovers keep, or the defaults when never saved.</summary>
    Task<PlannerSettings> GetPlannerAsync(CancellationToken cancellationToken);

    Task SavePlannerAsync(PlannerSettings settings, CancellationToken cancellationToken);

    /// <summary>Repeats, expiring food, perishables and the course words, or the defaults when never saved.</summary>
    Task<AdvancedSettings> GetAdvancedAsync(CancellationToken cancellationToken);

    Task SaveAdvancedAsync(AdvancedSettings settings, CancellationToken cancellationToken);

    /// <summary>When meals appear in the calendar feed, or the defaults when never saved.</summary>
    Task<CalendarSettings> GetCalendarTimesAsync(CancellationToken cancellationToken);

    Task SaveCalendarTimesAsync(CalendarSettings settings, CancellationToken cancellationToken);

    Task<List<GroceryStore>> ListStoresAsync(CancellationToken cancellationToken);

    /// <summary>Makes the saved stores exactly these, matched by id: others are removed.</summary>
    Task SaveStoresAsync(IReadOnlyList<GroceryStore> stores, CancellationToken cancellationToken);

    /// <summary>The to-do apps the shopping list can be sent to, in their saved order. Empty when none are set up.</summary>
    Task<List<ShoppingTarget>> ListShoppingTargetsAsync(CancellationToken cancellationToken);

    /// <summary>Makes the saved to-do apps exactly these, matched by id: others are removed.</summary>
    Task SaveShoppingTargetsAsync(IReadOnlyList<ShoppingTarget> targets, CancellationToken cancellationToken);

    /// <summary>The household's plan styles in their saved order. Empty when none are saved.</summary>
    Task<List<PlanStyle>> ListPlanStylesAsync(CancellationToken cancellationToken);

    /// <summary>Makes the saved plan styles exactly these, matched by id: others are removed.</summary>
    Task SavePlanStylesAsync(IReadOnlyList<PlanStyle> styles, CancellationToken cancellationToken);
}
