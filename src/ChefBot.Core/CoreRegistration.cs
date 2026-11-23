using System.Net.Sockets;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using ChefBot.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

namespace ChefBot.Core;

/// <summary>
/// Wires up everything behind the manager interfaces in <see cref="Managers.Contracts.Services"/>: managers, engines,
/// resource accessors and utilities, the PostgreSQL database and the message bus's storage in it.
/// </summary>
public static class CoreRegistration
{
    /// <summary>The schema that holds the message bus's own tables, next to ChefBot's.</summary>
    public const string MessagingSchema = "wolverine";

    /// <summary>
    /// How ChefBot introduces itself to recipe sites. Some sites turn away requests that don't look like a browser,
    /// so it starts the way browsers do and then says what it is.
    /// </summary>
    private const string WebPageUserAgent = "Mozilla/5.0 (compatible; ChefBot/1.0; self-hosted recipe import)";

    /// <summary>How long startup waits for PostgreSQL to accept connections before giving up.</summary>
    private static readonly TimeSpan DatabaseStartupWait = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddChefBotCore(this IServiceCollection services, IConfiguration configuration)
    {
        var planner = configuration.GetSection("Planner").Get<PlannerOptions>() ?? new PlannerOptions();
        planner.Validate();

        // Settings and reference data: loaded once, never changed while running.
        services.AddSingleton(planner);
        services.AddSingleton(IngredientCatalog.FromJson(File.ReadAllText(DataPath(configuration, "Data:IngredientCatalog", "config/ingredients.json"))));
        services.TryAddSingleton(TimeProvider.System);

        // Utilities.
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<PlanWriteLock>();

        // Resource access: PostgreSQL, the webhook, recipe pages and photos on the web, the backup folder, and the sample recipe, known store and starter plan style files.
        services.AddDbContext<ChefBotDbContext>(options => options.UseNpgsql(ConnectionString(configuration)));
        services.AddScoped<IRecipeAccess, RecipeAccess>();
        services.AddScoped<IPlanAccess, PlanAccess>();
        services.AddScoped<IPantryAccess, PantryAccess>();
        services.AddScoped<IShoppingListAccess, ShoppingListAccess>();
        services.AddScoped<IAccountAccess, AccountAccess>();
        services.AddScoped<IInboxAccess, InboxAccess>();
        services.AddScoped<ISettingsAccess, SettingsAccess>();
        services.AddScoped<IMessageAccess, MessageAccess>();
        services.AddScoped<IHistoryAccess, HistoryAccess>();
        services.AddScoped<IAchievementAccess, AchievementAccess>();
        services.AddScoped<IFavouriteAccess, FavouriteAccess>();
        services.AddScoped<IReviewAccess, ReviewAccess>();
        services.AddScoped<IRecipeSuggestionAccess, RecipeSuggestionAccess>();
        services.AddScoped<ICookbookAccess, CookbookAccess>();
        var backupFolder = StoragePath(configuration, "Data:Backups", "backups");
        services.AddScoped<IBackupAccess>(provider => new BackupAccess(provider.GetRequiredService<ChefBotDbContext>(), backupFolder));
        services.AddSingleton<ISampleRecipeAccess>(new SampleRecipeAccess(DataPath(configuration, "Data:SampleRecipes", "config/sample-recipes.json")));
        services.AddSingleton<IKnownStoreAccess>(new KnownStoreAccess(DataPath(configuration, "Data:KnownStores", "config/stores.json")));
        services.AddSingleton<IKnownPlanStyleAccess>(new KnownPlanStyleAccess(DataPath(configuration, "Data:PlanStyles", "config/plan-styles.json")));
        services.AddHttpClient<INotificationAccess, NotificationAccess>(client => client.Timeout = TimeSpan.FromSeconds(10));
        // To-do apps are set up by an admin and are often on the home network (Home Assistant), so private addresses are allowed.
        services.AddHttpClient<IToDoListAccess, ToDoListAccess>(client => client.Timeout = TimeSpan.FromSeconds(10));
        // AI models are set up by an admin and may run on the home network (Ollama, LM Studio), so private addresses
        // are allowed. Reading a photo or a long recipe can take a minute or more.
        services.AddHttpClient<IAiAccess, AiAccess>(client => client.Timeout = TimeSpan.FromSeconds(180));
        // Photos and recipe pages come from addresses people or web pages choose, so they only reach the public internet.
        services.AddHttpClient<IImageSourceAccess, ImageSourceAccess>(client => client.Timeout = TimeSpan.FromSeconds(20))
            .ConfigurePrimaryHttpMessageHandler(PublicWeb.Handler);
        services.AddHttpClient<IWebPageAccess, WebPageAccess>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(WebPageUserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(PublicWeb.Handler);

        // Engines. They hold no state; ValidationEngine reads through the accessors, so it lives per request.
        // Recipe file formats, chosen by file name ending and then by the first one that recognises the file.
        // Add a line here for each new kind of file; put specific formats before general ones like JsonRecipeFormat.
        services.AddSingleton<IRecipeFileFormat, MealieBackupFormat>();
        services.AddSingleton<IRecipeFileFormat, JsonRecipeFormat>();
        // Recipe page readers, chosen by the first that handles the page's address. Add a line here for each site
        // reader in Engines/Transformation/Sites/, before SchemaOrgPage, which reads any site.
        services.AddSingleton<SchemaRecipe>();
        services.AddSingleton<IRecipePageReader, SchemaOrgPage>();
        services.AddSingleton<TransformationEngine>();
        services.AddSingleton<PlanningEngine>();
        services.AddSingleton<ShoppingEngine>();
        services.AddSingleton<SuggestionEngine>();
        services.AddSingleton<SecurityEngine>();
        services.AddSingleton<NotificationEngine>();
        services.AddSingleton<CalendarEngine>();
        services.AddSingleton<HistoryEngine>();
        services.AddSingleton<AchievementEngine>();
        services.AddScoped<ValidationEngine>();

        // Managers: one per request or message, sharing one database transaction.
        services.AddScoped<IPlanManager, PlanManager>();
        services.AddScoped<IPantryManager, PantryManager>();
        services.AddScoped<IRecipeManager, RecipeManager>();
        services.AddScoped<IAccountManager, AccountManager>();
        services.AddScoped<IMaintenanceManager, MaintenanceManager>();

        return services;
    }

    /// <summary>
    /// Keeps the message bus's messages in PostgreSQL, in the <see cref="MessagingSchema"/> schema, so commands and
    /// events published by the managers are saved in the same transaction as their data and survive a restart.
    /// Their JSON comes from <see cref="ContractJsonContext"/>, made at build time.
    /// </summary>
    public static void UseChefBotMessageStorage(this WolverineOptions wolverine, IConfiguration configuration)
    {
        wolverine.UseSystemTextJsonForSerialization(json => json.TypeInfoResolverChain.Insert(0, ContractJsonContext.Default));
        wolverine.PersistMessagesWithPostgresql(ConnectionString(configuration), MessagingSchema);
        wolverine.UseEntityFrameworkCoreTransactions();
        wolverine.Policies.UseDurableLocalQueues();
        wolverine.UseChefBotQueues();
        wolverine.UseChefBotErrorPolicies();
    }

    /// <summary>
    /// Each handler of an event runs on its own (it can fail, retry or wait without holding up the others), on the
    /// queue named by its <c>[StickyHandler]</c>. Queue routing stays in this one method, so a queue can later move
    /// to another transport without touching the handlers.
    /// </summary>
    private static void UseChefBotQueues(this WolverineOptions wolverine)
    {
        wolverine.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
        wolverine.Durability.MessageIdentity = MessageIdentity.IdAndDestination;

        wolverine.LocalQueue(MessageQueues.Plans).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.History).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Achievements).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Maintenance).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Ai).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Images).UseDurableInbox().MaximumParallelMessages(2);
        wolverine.LocalQueue(MessageQueues.Notifications).UseDurableInbox().MaximumParallelMessages(2);
        wolverine.LocalQueue(MessageQueues.Inbox).UseDurableInbox().Sequential();
        wolverine.LocalQueue(MessageQueues.Live).BufferedInMemory();
    }

    /// <summary>
    /// Retries come from here, never from handlers. A database that hiccups is retried at once, then later; an
    /// outside service that is down is retried over the next half hour; anything else is a bug, and goes straight to
    /// the dead-letter table (wolverine_dead_letters), where it can be replayed once fixed.
    /// </summary>
    private static void UseChefBotErrorPolicies(this WolverineOptions wolverine)
    {
        wolverine.Policies.OnException<NpgsqlException>(exception => exception.IsTransient)
            .Or<TimeoutException>()
            .Or<DbUpdateException>(exception => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            .RetryWithCooldown(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1))
            .Then.ScheduleRetry(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
            .Then.MoveToErrorQueue();

        wolverine.Policies.OnException<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TransientDeliveryException>()
            .Or<AiUnavailableException>(exception => exception.IsTransient)
            .ScheduleRetry(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30))
            .Then.MoveToErrorQueue();

        wolverine.Policies.OnAnyException().MoveToErrorQueue();
    }

    /// <summary>
    /// Creates ChefBot's tables on first start and applies any newer migrations on later ones
    /// (<see cref="DatabaseUpgrade"/>). Waits for PostgreSQL to accept connections first, so ChefBot can start at the
    /// same time as its database. Other errors, such as a wrong password, fail straight away. Run it before the
    /// message bus starts, so no handler meets an old schema.
    /// </summary>
    public static async Task InitializeChefBotDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("ChefBot.Database");
        var giveUpAt = DateTimeOffset.UtcNow + DatabaseStartupWait;

        while (true)
        {
            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ChefBotDbContext>();
            var database = context.Database;
            try
            {
                await DatabaseUpgrade.RunAsync(context, logger, cancellationToken);
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

    /// <summary>
    /// Resolves a path to a file ChefBot ships with, such as the ingredient catalog, from settings. Relative paths are
    /// relative to the application folder.
    /// </summary>
    public static string DataPath(IConfiguration configuration, string key, string fallback) =>
        Resolve(AppContext.BaseDirectory, configuration[key] is { Length: > 0 } configured ? configured : fallback);

    /// <summary>
    /// The folder that holds everything ChefBot writes and must keep: its database, session keys, HTTPS certificate,
    /// backups and settings.json (Data:Root). A relative path is relative to the application folder, which is also the
    /// default; the container image sets /data.
    /// </summary>
    public static string DataRoot(IConfiguration configuration) => DataPath(configuration, "Data:Root", ".");

    /// <summary>Resolves a path to something ChefBot writes, from settings. Relative paths are relative to <see cref="DataRoot"/>.</summary>
    public static string StoragePath(IConfiguration configuration, string key, string fallback) =>
        Resolve(DataRoot(configuration), configuration[key] is { Length: > 0 } configured ? configured : fallback);

    private static string Resolve(string folder, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(folder, path));

    private static string ConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("ChefBot")
        ?? throw new InvalidOperationException("Set the ConnectionStrings__ChefBot setting to a PostgreSQL connection string.");

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
