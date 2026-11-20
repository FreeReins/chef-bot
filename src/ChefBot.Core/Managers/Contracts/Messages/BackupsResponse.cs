namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Backup settings and the backups that exist, newest first.</summary>
public sealed record BackupsResponse(
    bool Enabled,
    string Time,
    int KeepCount,
    int KeepDays,
    string LastResult,
    IReadOnlyList<BackupFile> Files);
