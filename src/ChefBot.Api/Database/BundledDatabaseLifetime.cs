namespace ChefBot.Api.Database;

/// <summary>
/// Starts the bundled PostgreSQL as the app starts, before the message bus and <see cref="ChefBotStartup"/>, and
/// stops it once everything else has stopped, the message bus included, which still writes to the database while it
/// stops. Both happen inside the host's start and stop, so a Windows service has already told Windows it is
/// starting while the database is first created, and isn't marked stopped while its database is still running.
/// </summary>
public sealed class BundledDatabaseLifetime(BundledDatabase database, ILogger<BundledDatabase> logger) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken) => database.StartAsync(logger, cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => database.StopAsync(logger);
}
