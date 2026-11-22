using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>Reminders sent to the notification webhook, and backups of all of ChefBot's data.</summary>
public interface IMaintenanceManager
{
    Task<NotificationSettingsResponse> GetNotificationSettingsAsync(CancellationToken cancellationToken);

    /// <summary>Saves the webhook address, which reminders are wanted and when the daily ones go out.</summary>
    Task<NotificationSettingsResponse> UpdateNotificationSettingsAsync(NotificationSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Sends a sample notification so the receiving side can be checked.</summary>
    Task<DeliveryResult> SendTestNotificationAsync(CancellationToken cancellationToken);

    /// <summary>Announces a newly staged plan.</summary>
    /// <exception cref="TransientDeliveryException">The webhook is unreachable for now; try again later.</exception>
    Task<ReminderRun> SendPlanReadyAsync(PlanStaged staged, CancellationToken cancellationToken);

    /// <summary>
    /// Queues today's reminders now, whatever the time, and marks today's as done in the same step. Each one is
    /// delivered on its own (<see cref="SendReminderAsync"/>); <see cref="ReminderRun.Sent"/> lists the ones queued.
    /// </summary>
    Task<ReminderRun> SendTodayAsync(CancellationToken cancellationToken);

    /// <summary>Delivers one queued reminder, worded as things are now; nothing when it no longer applies.</summary>
    /// <exception cref="TransientDeliveryException">The webhook is unreachable for now; try again later.</exception>
    Task<ReminderRun> SendReminderAsync(SendReminder command, CancellationToken cancellationToken);

    /// <summary>Announces new achievement tiers and chef ranks, when the household wants that.</summary>
    /// <exception cref="TransientDeliveryException">The webhook is unreachable for now; try again later.</exception>
    Task<ReminderRun> SendAchievementsAsync(AchievementsUnlocked unlocked, CancellationToken cancellationToken);

    /// <summary>
    /// Queues today's reminders if their time has come and they haven't gone out yet today (null when not due), then
    /// schedules the next check. A check that isn't the one saved as due does nothing, so only one is ever waiting.
    /// </summary>
    Task<ReminderRun?> SendDueRemindersAsync(SendDueReminders command, CancellationToken cancellationToken);

    /// <summary>How plans weigh repeats, expiring food and perishables, and the words that sort imported recipes.</summary>
    Task<AdvancedSettingsResponse> GetAdvancedSettingsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the advanced settings; anything left out goes back to its default. New course words apply to recipes
    /// imported afterwards. Publishes <see cref="PlanningSettingsChanged"/> so open plans follow.
    /// </summary>
    Task<AdvancedSettingsResponse> SaveAdvancedSettingsAsync(AdvancedSettingsRequest request, CancellationToken cancellationToken);

    Task<BackupsResponse> GetBackupsAsync(CancellationToken cancellationToken);

    /// <summary>Saves when backups run and how many are kept.</summary>
    Task<BackupsResponse> UpdateBackupSettingsAsync(BackupSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Makes a backup now, then deletes the ones that fall outside the retention rules.</summary>
    Task<BackupsResponse> RunBackupAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs the daily backup if it is switched on, its time has come and it hasn't run yet today (null when not due),
    /// then schedules the next check. A check that isn't the one saved as due does nothing.
    /// </summary>
    Task<BackupFile?> RunDueBackupAsync(RunDueBackup command, CancellationToken cancellationToken);

    /// <summary>Opens a backup file for download.</summary>
    Task<Stream> OpenBackupAsync(string name, CancellationToken cancellationToken);

    /// <summary>Counts what a backup holds, so it can be checked before restoring it.</summary>
    Task<BackupSummary> GetBackupSummaryAsync(string name, CancellationToken cancellationToken);

    Task<BackupsResponse> DeleteBackupAsync(string name, CancellationToken cancellationToken);

    /// <summary>Replaces all of ChefBot's data with a backup. Everything saved since that backup is lost.</summary>
    Task RestoreBackupAsync(string name, CancellationToken cancellationToken);
}
