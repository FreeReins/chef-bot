using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>Backup files: everything in the database as compressed JSON, kept in the backup folder.</summary>
internal interface IBackupAccess
{
    /// <summary>Writes everything in the database to a new backup file.</summary>
    Task<StoredBackup> CreateAsync(string name, CancellationToken cancellationToken);

    /// <summary>Every backup file, in any order.</summary>
    IReadOnlyList<StoredBackup> List();

    void Delete(string name);

    /// <summary>Opens a backup for download.</summary>
    Stream OpenRead(string name);

    /// <summary>Replaces everything in the database with the contents of a backup, in its own transaction.</summary>
    Task RestoreAsync(string name, CancellationToken cancellationToken);
}
