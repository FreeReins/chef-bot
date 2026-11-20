using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Tests;

internal sealed class FakeSettings : ISettingsAccess
{
    public SecuritySettings Security { get; set; } = new();

    public NotificationSettings Notifications { get; set; } = new();

    public BackupSettings Backups { get; set; } = new();

    public MealieSettings Mealie { get; set; } = new();

    public ScheduleState Schedule { get; set; } = new();

    public Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Security));

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) => Set(() => Security = Copies.Of(settings));

    public Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Notifications));

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) => Set(() => Notifications = Copies.Of(settings));

    public Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Backups));

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) => Set(() => Backups = Copies.Of(settings));

    public Task<MealieSettings> GetMealieAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Mealie));

    public Task SaveMealieAsync(MealieSettings settings, CancellationToken cancellationToken) => Set(() => Mealie = Copies.Of(settings));

    public Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Schedule));

    public Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken) => Set(() => Schedule = Copies.Of(state));

    private static Task Set(Action save)
    {
        save();
        return Task.CompletedTask;
    }
}
