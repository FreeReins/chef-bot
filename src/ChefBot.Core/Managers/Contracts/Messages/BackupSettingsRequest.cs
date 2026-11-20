namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>When backups run and how many are kept. Zero means no limit.</summary>
public sealed record BackupSettingsRequest(bool Enabled, string Time, int KeepCount, int KeepDays);
