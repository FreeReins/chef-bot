namespace ChefBot.Api;

/// <summary>
/// Prepares the database as the app starts (<see cref="ServiceRegistration.PrepareChefBotAsync"/>): after the
/// bundled PostgreSQL is up, and before the message bus starts and creates its own tables, because ChefBot's are
/// only created in a database that has none yet.
/// </summary>
public sealed class ChefBotStartup(IServiceProvider services) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken) => services.PrepareChefBotAsync();

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
