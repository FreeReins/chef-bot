using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Tests;

internal sealed class NoBackups : IBackupAccess
{
    public Task<StoredBackup> CreateAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();

    public IReadOnlyList<StoredBackup> List() => [];

    public void Delete(string name) => throw new NotSupportedException();

    public Stream OpenRead(string name) => throw new NotSupportedException();

    public Task RestoreAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
}
