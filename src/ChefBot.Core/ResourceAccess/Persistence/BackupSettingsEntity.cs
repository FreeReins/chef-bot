namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class BackupSettingsEntity
{
    public int Id { get; set; } = 1;

    public bool Enabled { get; set; }

    public string Time { get; set; } = string.Empty;

    public int KeepCount { get; set; }

    public int KeepDays { get; set; }

    public DateOnly? LastRunOn { get; set; }

    public string LastResult { get; set; } = string.Empty;
}
