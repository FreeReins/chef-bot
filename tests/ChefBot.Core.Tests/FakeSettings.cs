using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeSettings : ISettingsAccess
{
    public SecuritySettings Security { get; set; } = new();

    public NotificationSettings Notifications { get; set; } = new();

    public BackupSettings Backups { get; set; } = new();

    public ScheduleState Schedule { get; set; } = new();

    public List<GroceryStore> Stores { get; set; } = [];

    public List<PlanStyle> Styles { get; set; } = [];

    public List<ShoppingTarget> ShoppingTargets { get; set; } = [];

    public Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Security));

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) => Set(() => Security = Copies.Of(settings));

    public Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Notifications));

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) => Set(() => Notifications = Copies.Of(settings));

    public Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Backups));

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) => Set(() => Backups = Copies.Of(settings));

    public Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Schedule));

    public Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken) => Set(() => Schedule = Copies.Of(state));

    public Task<List<GroceryStore>> ListStoresAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Stores.Select(Copies.Of).OrderBy(store => store.Position).ToList());

    public Task SaveStoresAsync(IReadOnlyList<GroceryStore> stores, CancellationToken cancellationToken) =>
        Set(() => Stores = stores.Select(Copies.Of).ToList());

    public Task<List<ShoppingTarget>> ListShoppingTargetsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ShoppingTargets.Select(Copies.Of).OrderBy(target => target.Position).ToList());

    public Task SaveShoppingTargetsAsync(IReadOnlyList<ShoppingTarget> targets, CancellationToken cancellationToken) =>
        Set(() => ShoppingTargets = targets.Select(Copies.Of).ToList());

    public Task<List<PlanStyle>> ListPlanStylesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Styles.Select(Copies.Of).OrderBy(style => style.Position).ToList());

    public Task SavePlanStylesAsync(IReadOnlyList<PlanStyle> styles, CancellationToken cancellationToken) =>
        Set(() => Styles = styles.Select(Copies.Of).ToList());

    private static Task Set(Action save)
    {
        save();
        return Task.CompletedTask;
    }
}
