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

    /// <summary>The household's stores in their saved order. Empty when none are set up.</summary>
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
