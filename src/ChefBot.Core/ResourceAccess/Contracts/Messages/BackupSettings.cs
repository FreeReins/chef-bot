namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>When backups run and how many are kept.</summary>
internal sealed class BackupSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Local time the daily backup runs, as HH:mm.</summary>
    public string Time { get; set; } = "03:00";

    /// <summary>Keep this many of the newest backups. Zero keeps any number.</summary>
    public int KeepCount { get; set; } = 14;

    /// <summary>Delete backups older than this many days. Zero keeps them whatever their age.</summary>
    public int KeepDays { get; set; }

    /// <summary>The last day the scheduled backup ran, so it runs once a day.</summary>
    public DateOnly? LastRunOn { get; set; }

    /// <summary>How the last backup went, in a sentence.</summary>
    public string LastResult { get; set; } = string.Empty;

    /// <summary>When the scheduled check for the daily backup is due. A queued check due at another time is stale.</summary>
    public DateTimeOffset? DueAt { get; set; }
}
