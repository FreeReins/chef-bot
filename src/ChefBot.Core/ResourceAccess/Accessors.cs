using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

// The resource accessors: the only code that touches PostgreSQL, Mealie, the webhook, the backup folder and the
// sample recipe file. Each takes and returns the plain data contracts in ResourceAccess/Contracts. Database writes
// are held until a manager calls IUnitOfWork.CommitAsync.

/// <summary>Stored recipes.</summary>
internal interface IRecipeAccess
{
    /// <summary>Recipes that can be planned, with their ingredients, by name.</summary>
    Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Every recipe, archived ones included.</summary>
    Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken);

    Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds the recipe, or updates the stored one with the same id.</summary>
    Task SaveAsync(Recipe recipe, CancellationToken cancellationToken);
}

/// <summary>Stored weekly plans, always loaded whole: meals with their recipes, and the shopping list.</summary>
internal interface IPlanAccess
{
    Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken);

    /// <summary>The plan covering <paramref name="today"/>, or else the next upcoming one.</summary>
    Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Plans whose week hasn't ended by <paramref name="today"/>.</summary>
    Task<List<MealPlan>> ListOpenAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Recipes used by plans whose week starts in [<paramref name="from"/>, <paramref name="before"/>).</summary>
    Task<HashSet<Guid>> RecipeIdsPlannedAsync(DateOnly from, DateOnly before, CancellationToken cancellationToken);

    /// <summary>Adds the plan, or updates the stored one with the same id, including its meals and shopping list.</summary>
    Task SaveAsync(MealPlan plan, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>What is in the kitchen right now.</summary>
internal interface IPantryAccess
{
    Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken);

    Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(PantryItem item, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Stored accounts and their invite and password reset links.</summary>
internal interface IAccountAccess
{
    Task<bool> AnyUsersAsync(CancellationToken cancellationToken);

    Task<UserAccount?> FindUserAsync(Guid id, CancellationToken cancellationToken);

    /// <param name="username">The normalized (lower-case) sign-in name.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    Task<UserAccount?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<List<UserAccount>> ListUsersAsync(CancellationToken cancellationToken);

    Task SaveUserAsync(UserAccount user, CancellationToken cancellationToken);

    Task RemoveUserAsync(Guid id, CancellationToken cancellationToken);

    Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken);

    Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken);

    Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken);

    Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken);
}

/// <summary>Settings an admin can change while ChefBot is running. Defaults are returned when none are saved yet.</summary>
internal interface ISettingsAccess
{
    Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken);

    Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken);

    Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken);

    Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken);

    Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken);

    Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken);

    Task<MealieSettings> GetMealieAsync(CancellationToken cancellationToken);

    Task SaveMealieAsync(MealieSettings settings, CancellationToken cancellationToken);

    Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken);

    Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken);
}

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

/// <summary>An outside recipe library that can be imported: a Mealie instance.</summary>
internal interface IRecipeSourceAccess
{
    /// <summary>Reads every recipe, using the address and token saved in <paramref name="connection"/>.</summary>
    /// <exception cref="ChefBot.Core.Contracts.RecipeSourceException">The source couldn't be reached or read.</exception>
    Task<RecipeSourceResult> FetchAllAsync(MealieSettings connection, CancellationToken cancellationToken);
}

/// <summary>Delivers notifications to the webhook.</summary>
internal interface INotificationAccess
{
    /// <summary>Posts a notification as JSON. Failures are reported in the result, never thrown.</summary>
    Task<Delivery> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken);
}

/// <summary>The starter recipes offered by the setup wizard.</summary>
internal interface ISampleRecipeAccess
{
    IReadOnlyList<RecipeText> All();
}

/// <summary>
/// One transaction: every database change made through the accessors, and every message published, is saved
/// together by <see cref="CommitAsync"/>. Messages are only sent once the data they describe is saved.
/// </summary>
internal interface IUnitOfWork
{
    /// <summary>Queues a command or event, sent when the transaction commits.</summary>
    Task PublishAsync(object message);

    Task CommitAsync(CancellationToken cancellationToken);
}
