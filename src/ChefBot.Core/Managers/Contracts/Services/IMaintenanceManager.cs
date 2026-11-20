using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>Reminders sent to the notification webhook, and backups of all of ChefBot's data.</summary>
public interface IMaintenanceManager
{
    Task<NotificationSettingsResponse> GetNotificationSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Saves the webhook address, which reminders are wanted and when the daily ones go out.</summary>
    Task<NotificationSettingsResponse> UpdateNotificationSettingsAsync(NotificationSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Uses a webhook from the configuration as the starting value, when none has been saved.</summary>
    Task UseDefaultWebhookAsync(string? webhookUrl, CancellationToken cancellationToken);

    /// <summary>Sends a sample notification so the receiving side can be checked.</summary>
    Task<DeliveryResult> SendTestNotificationAsync(CancellationToken cancellationToken);

    /// <summary>Announces a newly staged plan.</summary>
    Task<ReminderRun> SendPlanReadyAsync(PlanStaged staged, CancellationToken cancellationToken);

    /// <summary>Sends today's reminders now, whatever the time.</summary>
    Task<ReminderRun> SendTodayAsync(CancellationToken cancellationToken);

    /// <summary>Sends today's reminders if their time has come and they haven't gone out yet today. Null when not due.</summary>
    Task<ReminderRun?> SendDueRemindersAsync(CancellationToken cancellationToken);

    Task<BackupsResponse> GetBackupsAsync(CancellationToken cancellationToken);

    /// <summary>Saves when backups run and how many are kept.</summary>
    Task<BackupsResponse> UpdateBackupSettingsAsync(BackupSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Makes a backup now, then deletes the ones that fall outside the retention rules.</summary>
    Task<BackupsResponse> RunBackupAsync(CancellationToken cancellationToken);

    /// <summary>Runs the daily backup if it is switched on, its time has come and it hasn't run yet today. Null when not due.</summary>
    Task<BackupFile?> RunDueBackupAsync(CancellationToken cancellationToken);

    /// <summary>Opens a backup file for download.</summary>
    Task<Stream> OpenBackupAsync(string name, CancellationToken cancellationToken);

    Task<BackupsResponse> DeleteBackupAsync(string name, CancellationToken cancellationToken);

    /// <summary>Replaces all of ChefBot's data with a backup. Everything saved since that backup is lost.</summary>
    Task RestoreBackupAsync(string name, CancellationToken cancellationToken);
}
