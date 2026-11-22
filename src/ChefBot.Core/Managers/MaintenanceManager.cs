using System.Text.Json;
using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IMaintenanceManager"/>
internal sealed class MaintenanceManager(
    ISettingsAccess settings,
    IPlanAccess plans,
    IPantryAccess pantry,
    INotificationAccess webhook,
    IBackupAccess backups,
    IMessageAccess messages,
    NotificationEngine notifications,
    ValidationEngine validation,
    TimeProvider clock) : IMaintenanceManager
{
    // ---------- notifications ----------

    public async Task<NotificationSettingsResponse> GetNotificationSettingsAsync(CancellationToken cancellationToken) =>
        (await settings.GetNotificationsAsync(cancellationToken)).ToResponse();

    public async Task<NotificationSettingsResponse> UpdateNotificationSettingsAsync(NotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        var url = ValidationEngine.Webhook(request.WebhookUrl);
        var time = ValidationEngine.TimeOfDay(request.DailyTime);

        var current = await settings.GetNotificationsAsync(cancellationToken);
        current.WebhookUrl = url;
        current.PlanReady = request.PlanReady;
        current.SlowCooker = request.SlowCooker;
        current.MealsToday = request.MealsToday;
        current.ExpiringFood = request.ExpiringFood;
        current.Shopping = request.Shopping;
        current.DailyTime = time;
        await settings.SaveNotificationsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return current.ToResponse();
    }

    public async Task<DeliveryResult> SendTestNotificationAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        if (current.WebhookUrl.Length == 0)
        {
            return new DeliveryResult(false, "Enter a webhook address and save it first.");
        }

        return (await webhook.SendAsync(current.WebhookUrl, notifications.Test(), cancellationToken)).ToResponse();
    }

    // ---------- advanced settings ----------

    public async Task<AdvancedSettingsResponse> GetAdvancedSettingsAsync(CancellationToken cancellationToken) =>
        (await settings.GetAdvancedAsync(cancellationToken)).ToResponse();

    public async Task<AdvancedSettingsResponse> SaveAdvancedSettingsAsync(AdvancedSettingsRequest request, CancellationToken cancellationToken)
    {
        var fresh = ValidationEngine.Advanced(request);
        await settings.SaveAdvancedAsync(fresh, cancellationToken);
        await messages.PublishAsync(new PlanningSettingsChanged());
        await messages.CommitAsync(cancellationToken);
        fresh.IsSaved = true;
        return fresh.ToResponse();
    }

    public async Task<ReminderRun> SendPlanReadyAsync(PlanStaged staged, CancellationToken cancellationToken)
    {
        // The plan may have been replaced or cleared since it was staged.
        var plan = await plans.FindAsync(staged.PlanId, cancellationToken);
        return plan is null
            ? new ReminderRun([], [])
            : await SendAsync([notifications.PlanReady(plan, staged.Trigger)], cancellationToken);
    }

    public async Task<ReminderRun> SendTodayAsync(CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var plan = await plans.FindCurrentAsync(today, cancellationToken);
        var reminders = notifications.Daily(plan, await pantry.ListAsync(cancellationToken), today, (await settings.GetAdvancedAsync(cancellationToken)).ExpiringSoonDays);
        var run = await SendAsync(reminders, cancellationToken);

        var current = await settings.GetNotificationsAsync(cancellationToken);
        current.LastDailyRunOn = today;
        await settings.SaveNotificationsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return run;
    }

    public async Task<ReminderRun?> SendDueRemindersAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        return DailyTime.IsDue(current.DailyTime, current.LastDailyRunOn, clock.GetLocalNow())
            ? await SendTodayAsync(cancellationToken)
            : null;
    }

    // ---------- backups ----------

    public Task<BackupsResponse> GetBackupsAsync(CancellationToken cancellationToken) => DescribeBackupsAsync(cancellationToken);

    public async Task<BackupsResponse> UpdateBackupSettingsAsync(BackupSettingsRequest request, CancellationToken cancellationToken)
    {
        ValidationEngine.BackupLimits(request.KeepCount, request.KeepDays);
        var time = ValidationEngine.TimeOfDay(request.Time);

        var current = await settings.GetBackupsAsync(cancellationToken);
        current.Enabled = request.Enabled;
        current.Time = time;
        current.KeepCount = request.KeepCount;
        current.KeepDays = request.KeepDays;
        await settings.SaveBackupsAsync(current, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await DescribeBackupsAsync(cancellationToken);
    }

    public async Task<BackupsResponse> RunBackupAsync(CancellationToken cancellationToken)
    {
        await BackUpAsync(cancellationToken);
        return await DescribeBackupsAsync(cancellationToken);
    }

    public async Task<BackupFile?> RunDueBackupAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetBackupsAsync(cancellationToken);
        return current.Enabled && DailyTime.IsDue(current.Time, current.LastRunOn, clock.GetLocalNow())
            ? (await BackUpAsync(cancellationToken)).ToResponse()
            : null;
    }

    public Task<Stream> OpenBackupAsync(string name, CancellationToken cancellationToken) =>
        Task.FromResult(backups.OpenRead(validation.ExistingBackup(name)));

    public async Task<BackupSummary> GetBackupSummaryAsync(string name, CancellationToken cancellationToken)
    {
        var file = validation.ExistingBackup(name);
        try
        {
            return (await backups.CountAsync(file, cancellationToken)).ToResponse();
        }
        catch (Exception exception) when (exception is InvalidDataException or JsonException)
        {
            throw new RuleViolationException("This backup couldn't be read. It may be damaged: download it to check, or delete it.");
        }
    }

    public async Task<BackupsResponse> DeleteBackupAsync(string name, CancellationToken cancellationToken)
    {
        backups.Delete(validation.ExistingBackup(name));
        return await DescribeBackupsAsync(cancellationToken);
    }

    public Task RestoreBackupAsync(string name, CancellationToken cancellationToken) =>
        backups.RestoreAsync(validation.ExistingBackup(name), cancellationToken);

    /// <summary>Sends the notifications the household wants to the webhook, if one is set.</summary>
    private async Task<ReminderRun> SendAsync(IReadOnlyList<Notification> batch, CancellationToken cancellationToken)
    {
        var current = await settings.GetNotificationsAsync(cancellationToken);
        var sent = new List<string>();
        var problems = new List<string>();
        if (current.WebhookUrl.Length == 0)
        {
            return new ReminderRun(sent, problems);
        }

        foreach (var notification in batch.Where(notification => notifications.IsWanted(current, notification.Type)))
        {
            var result = await webhook.SendAsync(current.WebhookUrl, notification, cancellationToken);
            if (result.Delivered)
            {
                sent.Add(notification.Type);
            }
            else
            {
                problems.Add($"{notification.Type}: {result.Problem}");
            }
        }

        return new ReminderRun(sent, problems);
    }

    /// <summary>Makes a backup now, then deletes the ones that fall outside the retention rules. The outcome is saved either way.</summary>
    private async Task<StoredBackup> BackUpAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetBackupsAsync(cancellationToken);
        var now = clock.GetLocalNow();
        current.LastRunOn = DateOnly.FromDateTime(now.DateTime);

        try
        {
            var file = await backups.CreateAsync(notifications.BackupName(now), cancellationToken);
            var expired = notifications.ExpiredBackups(backups.List(), current, now);
            foreach (var old in expired)
            {
                backups.Delete(old.Name);
            }

            current.LastResult = $"Backed up on {now:yyyy-MM-dd HH:mm}. Removed {expired.Count} old backups.";
            return file;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            current.LastResult = $"The backup on {now:yyyy-MM-dd HH:mm} failed: {exception.Message}";
            throw new RuleViolationException($"The backup couldn't be written: {exception.Message}");
        }
        finally
        {
            await settings.SaveBackupsAsync(current, cancellationToken);
            await messages.CommitAsync(cancellationToken);
        }
    }

    private async Task<BackupsResponse> DescribeBackupsAsync(CancellationToken cancellationToken)
    {
        var current = await settings.GetBackupsAsync(cancellationToken);
        var files = backups.List().OrderByDescending(file => file.CreatedAt).Select(file => file.ToResponse()).ToList();
        return new BackupsResponse(current.Enabled, current.Time, current.KeepCount, current.KeepDays, current.LastResult, files);
    }
}
