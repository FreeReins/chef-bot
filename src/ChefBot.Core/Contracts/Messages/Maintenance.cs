namespace ChefBot.Core.Contracts.Messages;

// Requests, responses and commands of IMaintenanceManager: notifications and backups.

/// <summary>Where notifications go, which ones are wanted, and when the daily ones go out.</summary>
/// <param name="WebhookUrl">A URL that accepts a JSON POST, such as a Home Assistant webhook. Empty turns notifications off.</param>
/// <param name="PlanReady">Announce each newly staged plan.</param>
/// <param name="SlowCooker">Remind in the morning when tonight's dinner is a slow cooker meal.</param>
/// <param name="MealsToday">List what is planned for today.</param>
/// <param name="ExpiringFood">Warn about pantry food that is about to expire.</param>
/// <param name="Shopping">Remind to confirm the plan or finish the shopping as the week starts.</param>
/// <param name="DailyTime">Local time the daily reminders are sent, as HH:mm.</param>
public sealed record NotificationSettingsRequest(
    string? WebhookUrl,
    bool PlanReady,
    bool SlowCooker,
    bool MealsToday,
    bool ExpiringFood,
    bool Shopping,
    string DailyTime);

/// <summary>The notification settings, and the last day the daily reminders went out.</summary>
public sealed record NotificationSettingsResponse(
    string WebhookUrl,
    bool PlanReady,
    bool SlowCooker,
    bool MealsToday,
    bool ExpiringFood,
    bool Shopping,
    string DailyTime,
    DateOnly? LastDailyRunOn);

/// <summary>Whether a notification reached the webhook.</summary>
public sealed record DeliveryResult(bool Delivered, string? Problem = null);

/// <summary>The outcome of sending a batch of reminders.</summary>
/// <param name="Sent">The types that were delivered.</param>
/// <param name="Problems">What went wrong for those that weren't.</param>
public sealed record ReminderRun(IReadOnlyList<string> Sent, IReadOnlyList<string> Problems);

/// <summary>When backups run and how many are kept. Zero means no limit.</summary>
public sealed record BackupSettingsRequest(bool Enabled, string Time, int KeepCount, int KeepDays);

/// <summary>A backup file.</summary>
public sealed record BackupFile(string Name, DateTimeOffset CreatedAt, long SizeBytes);

/// <summary>Backup settings and the backups that exist, newest first.</summary>
public sealed record BackupsResponse(
    bool Enabled,
    string Time,
    int KeepCount,
    int KeepDays,
    string LastResult,
    IReadOnlyList<BackupFile> Files);

// ---------- commands (sent through the message bus) ----------

/// <summary>Command, every few minutes: send today's reminders if their time has come.</summary>
public sealed record SendDueReminders;

/// <summary>Command, every few minutes: run the daily backup if its time has come.</summary>
public sealed record RunDueBackup;
