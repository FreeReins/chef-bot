using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>Settings an admin can change while ChefBot is running. Defaults are returned when none are saved yet.</summary>
internal interface ISettingsAccess
{
    Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken);

    Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken);

    Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken);

    Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken);

    Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken);

    Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken);

    Task<MealieSettings> GetMealieAsync(CancellationToken cancellationToken);

    Task SaveMealieAsync(MealieSettings settings, CancellationToken cancellationToken);

    Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken);

    Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken);
}
