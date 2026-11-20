using System.Net.Http.Headers;
using System.Net.Sockets;
using ChefBot.Core.Abstractions;
using ChefBot.Core.Backups;
using ChefBot.Core.Notifications;
using ChefBot.Infrastructure.Accounts;
using ChefBot.Infrastructure.Mealie;
using ChefBot.Infrastructure.Notifications;
using ChefBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ChefBot.Infrastructure;

/// <summary>Registers everything that talks to the outside world: PostgreSQL, Mealie and the webhook.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddChefBotInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChefBot")
            ?? throw new InvalidOperationException("Set the ConnectionStrings__ChefBot setting to a PostgreSQL connection string.");

        services.AddDbContext<ChefBotDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IPantryRepository, PantryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAccessLinkRepository, AccessLinkRepository>();
        services.AddScoped<ISettingsStore, SettingsStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        services.Configure<MealieOptions>(configuration.GetSection("Mealie"));
        services.AddHttpClient<IRecipeSource, MealieRecipeSource>((provider, client) =>
        {
            var mealie = provider.GetRequiredService<IOptions<MealieOptions>>().Value;
            if (mealie.IsConfigured)
            {
                // The trailing slash keeps a path prefix such as /mealie/ when relative paths are added.
                client.BaseAddress = new Uri(mealie.Url.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mealie.ApiToken);
            }

            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<INotificationSender, WebhookNotificationSender>(client => client.Timeout = TimeSpan.FromSeconds(10));

        var backups = configuration["Data:Backups"] is { Length: > 0 } folder ? folder : "backups";
        services.AddScoped<IBackupStore>(provider => new DatabaseBackupStore(
            provider.GetRequiredService<ChefBotDbContext>(),
            Path.IsPathRooted(backups) ? backups : Path.Combine(AppContext.BaseDirectory, backups)));

        return services;
    }

    /// <summary>How long startup waits for PostgreSQL to accept connections before giving up.</summary>
    private static readonly TimeSpan DatabaseStartupWait = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Creates the database tables on first start. Waits for PostgreSQL to accept connections first, so ChefBot
    /// can start at the same time as its database. Other errors, such as a wrong password, fail straight away.
    /// </summary>
    public static async Task InitializeChefBotDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ChefBot.Database");
        var giveUpAt = DateTimeOffset.UtcNow + DatabaseStartupWait;

        while (true)
        {
            await using var scope = services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ChefBotDbContext>().Database;
            try
            {
                await database.EnsureCreatedAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (IsUnreachable(exception))
            {
                var server = DescribeServer(database.GetConnectionString());
                if (DateTimeOffset.UtcNow >= giveUpAt)
                {
                    throw new InvalidOperationException(
                        $"PostgreSQL at {server} didn't accept connections within {DatabaseStartupWait.TotalSeconds:0} seconds. " +
                        "Check that the database is running and that ConnectionStrings:ChefBot points at it.", exception);
                }

                logger.LogWarning("Waiting for PostgreSQL at {Server}: {Reason}", server, exception.GetBaseException().Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    /// <summary>True when the database can't be reached yet, as opposed to rejecting what was sent.</summary>
    private static bool IsUnreachable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException or NpgsqlException { IsTransient: true })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Host and port from a connection string, for messages. Never includes the password.</summary>
    private static string DescribeServer(string? connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return $"{builder.Host}:{builder.Port}";
    }
}
