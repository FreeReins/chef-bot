using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Backs the database up to compressed JSON files and restores from them. The files hold
/// ChefBot's own data rather than a PostgreSQL dump, so they don't depend on database tools
/// or versions. They include password hashes: treat them as private. The message bus's own
/// tables are not included.
/// </summary>
internal sealed partial class BackupAccess(ChefBotDbContext db, string folder) : IBackupAccess
{
    private const string Pattern = "chefbot-backup-*.json.gz";

    public async Task<StoredBackup> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var contents = new BackupContents
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Recipes = await db.Recipes.AsNoTracking()
                .Include(recipe => recipe.Ingredients)
                .Include(recipe => recipe.Images)
                .AsSplitQuery()
                .ToListAsync(cancellationToken),
            RecipeImageData = await db.RecipeImageData.AsNoTracking().ToListAsync(cancellationToken),
            MealPlans = await db.MealPlans.AsNoTracking()
                .Include(plan => plan.Meals).ThenInclude(meal => meal.CustomIngredients)
                .Include(plan => plan.ShoppingItems)
                .AsSplitQuery()
                .ToListAsync(cancellationToken),
            PantryItems = await db.PantryItems.AsNoTracking().ToListAsync(cancellationToken),
            ManualShoppingItems = await db.ManualShoppingItems.AsNoTracking().ToListAsync(cancellationToken),
            Users = await db.Users.AsNoTracking().ToListAsync(cancellationToken),
            Security = await db.Set<SecuritySettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Notifications = await db.Set<NotificationSettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Backups = await db.Set<BackupSettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Schedule = await db.Set<ScheduleStateEntity>().AsNoTracking().ToListAsync(cancellationToken),
            GroceryStores = await db.GroceryStores.AsNoTracking().ToListAsync(cancellationToken),
            PlanStyles = await db.PlanStyles.AsNoTracking().ToListAsync(cancellationToken),
            ShoppingTargets = await db.ShoppingTargets.AsNoTracking().ToListAsync(cancellationToken),
            CalendarFeed = await db.Set<CalendarFeedEntity>().AsNoTracking().ToListAsync(cancellationToken),
            FoodMoments = await db.FoodMoments.AsNoTracking().ToListAsync(cancellationToken),
            RecipeFavourites = await db.RecipeFavourites.AsNoTracking().ToListAsync(cancellationToken),
        };

        // A planned meal points at its recipe by id; the recipe itself is written once, under Recipes.
        foreach (var meal in contents.MealPlans.SelectMany(plan => plan.Meals))
        {
            meal.Recipe = null;
        }

        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        await using (var file = File.Create(path))
        await using (var zip = new GZipStream(file, CompressionLevel.Optimal))
        {
            await JsonSerializer.SerializeAsync(zip, contents, BackupJson.Default.BackupContents, cancellationToken);
        }

        return Describe(new FileInfo(path));
    }

    public IReadOnlyList<StoredBackup> List() =>
        Directory.Exists(folder)
            ? new DirectoryInfo(folder).GetFiles(Pattern).Select(Describe).ToList()
            : [];

    public void Delete(string name) => File.Delete(Path.Combine(folder, name));

    public Stream OpenRead(string name) => File.OpenRead(Path.Combine(folder, name));

    public async Task<BackupCounts> CountAsync(string name, CancellationToken cancellationToken)
    {
        BackupOutline outline;
        await using (var file = File.OpenRead(Path.Combine(folder, name)))
        await using (var zip = new GZipStream(file, CompressionMode.Decompress))
        {
            outline = await JsonSerializer.DeserializeAsync(zip, BackupJson.Default.BackupOutline, cancellationToken)
                ?? throw new InvalidDataException("The backup file is empty.");
        }

        return new BackupCounts(
            outline.Recipes.Count,
            outline.RecipeImageData.Count,
            outline.MealPlans.Count,
            outline.MealPlans.Sum(plan => plan.Meals.Count),
            outline.MealPlans.Sum(plan => plan.ShoppingItems.Count),
            outline.PantryItems.Count,
            outline.ManualShoppingItems.Count,
            outline.Users.Count,
            outline.GroceryStores.Count,
            outline.PlanStyles.Count);
    }

    public async Task RestoreAsync(string name, CancellationToken cancellationToken)
    {
        BackupContents contents;
        await using (var file = File.OpenRead(Path.Combine(folder, name)))
        await using (var zip = new GZipStream(file, CompressionMode.Decompress))
        {
            contents = await JsonSerializer.DeserializeAsync(zip, BackupJson.Default.BackupContents, cancellationToken)
                ?? throw new InvalidDataException("The backup file is empty.");
        }

        // Older backups wrote each meal's recipe as well. It is stored once, under Recipes.
        foreach (var meal in contents.MealPlans.SelectMany(plan => plan.Meals))
        {
            meal.Recipe = null;
        }

        // One transaction: either everything is replaced, or nothing changes.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Everyone signs in again after a restore. Passwords go back to the backup's, and so would the session
        // versions, which would bring back sessions that a password change since then had ended (a stolen cookie,
        // say). So each account's version moves past both its current one and the backup's, which no cookie
        // carries. An account removed since the backup has no current version; its sessions ended when it was
        // removed, unless its password also changed between the backup and the removal.
        var currentVersions = await db.Users.AsNoTracking().ToDictionaryAsync(user => user.Id, user => user.SessionVersion, cancellationToken);
        foreach (var user in contents.Users)
        {
            user.SessionVersion = Math.Max(user.SessionVersion, currentVersions.GetValueOrDefault(user.Id)) + 1;
        }

        // Children go with their parents: the database deletes them through its foreign keys.
        await db.AccessLinks.ExecuteDeleteAsync(cancellationToken);
        // API tokens aren't backed up, so a token revoked since the backup can't come back: like everyone signing
        // in again, assistants need new tokens after a restore.
        await db.ApiTokens.ExecuteDeleteAsync(cancellationToken);
        await db.MealPlans.ExecuteDeleteAsync(cancellationToken);
        await db.Recipes.ExecuteDeleteAsync(cancellationToken);
        await db.PantryItems.ExecuteDeleteAsync(cancellationToken);
        await db.ManualShoppingItems.ExecuteDeleteAsync(cancellationToken);
        await db.Users.ExecuteDeleteAsync(cancellationToken);
        await db.Set<SecuritySettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<NotificationSettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<BackupSettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<ScheduleStateEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.GroceryStores.ExecuteDeleteAsync(cancellationToken);
        await db.PlanStyles.ExecuteDeleteAsync(cancellationToken);
        await db.ShoppingTargets.ExecuteDeleteAsync(cancellationToken);
        await db.Set<CalendarFeedEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.FoodMoments.ExecuteDeleteAsync(cancellationToken);
        await db.RecipeFavourites.ExecuteDeleteAsync(cancellationToken);

        db.ChangeTracker.Clear();
        db.AddRange(contents.Recipes);
        db.AddRange(contents.RecipeImageData);
        db.AddRange(contents.MealPlans);
        db.AddRange(contents.PantryItems);
        db.AddRange(contents.ManualShoppingItems);
        db.AddRange(contents.Users);
        db.AddRange(contents.Security);
        db.AddRange(contents.Notifications);
        db.AddRange(contents.Backups);
        db.AddRange(contents.Schedule);
        db.AddRange(contents.GroceryStores);
        db.AddRange(contents.PlanStyles);
        db.AddRange(contents.ShoppingTargets);
        db.AddRange(contents.CalendarFeed);
        db.AddRange(contents.FoodMoments);
        db.AddRange(contents.RecipeFavourites);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static StoredBackup Describe(FileInfo file) =>
        new(file.Name, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero), file.Length);

    /// <summary>Everything a backup file holds.</summary>
    private sealed class BackupContents
    {
        public DateTimeOffset CreatedAt { get; set; }

        public List<RecipeEntity> Recipes { get; set; } = [];

        /// <summary>The recipe photos' bytes. Empty in backups made before ChefBot kept photos.</summary>
        public List<RecipeImageDataEntity> RecipeImageData { get; set; } = [];

        public List<MealPlanEntity> MealPlans { get; set; } = [];

        public List<PantryItemEntity> PantryItems { get; set; } = [];

        /// <summary>Empty in backups made before things could be added to the shopping list by hand.</summary>
        public List<ManualShoppingItemEntity> ManualShoppingItems { get; set; } = [];

        public List<UserEntity> Users { get; set; } = [];

        public List<SecuritySettingsEntity> Security { get; set; } = [];

        public List<NotificationSettingsEntity> Notifications { get; set; } = [];

        public List<BackupSettingsEntity> Backups { get; set; } = [];

        /// <summary>Empty in backups made before the weekly schedule remembered what it staged.</summary>
        public List<ScheduleStateEntity> Schedule { get; set; } = [];

        /// <summary>Empty in backups made before stores could be chosen in the web page.</summary>
        public List<GroceryStoreEntity> GroceryStores { get; set; } = [];

        /// <summary>Empty in backups made before households could keep their own plan styles.</summary>
        public List<PlanStyleEntity> PlanStyles { get; set; } = [];

        /// <summary>The to-do apps, with their access tokens. Empty in backups made before the shopping list could be sent to one.</summary>
        public List<ShoppingTargetEntity> ShoppingTargets { get; set; } = [];

        /// <summary>The calendar feed's secret. Empty in backups made before the meal plan had a calendar feed.</summary>
        public List<CalendarFeedEntity> CalendarFeed { get; set; } = [];

        /// <summary>The food history. Empty in backups made before ChefBot kept one.</summary>
        public List<FoodMomentEntity> FoodMoments { get; set; } = [];

        /// <summary>Who has which recipe as a favourite. Empty in backups made before favourites.</summary>
        public List<RecipeFavouriteEntity> RecipeFavourites { get; set; } = [];
    }

    /// <summary>
    /// Just enough of a backup file to count what it holds. The records' fields are skipped, so
    /// no entities or photo bytes are built. A list an older backup lacks reads as empty.
    /// </summary>
    private sealed class BackupOutline
    {
        public List<Counted> Recipes { get; init => field = value ?? []; } = [];

        public List<Counted> RecipeImageData { get; init => field = value ?? []; } = [];

        public List<PlanOutline> MealPlans { get; init => field = value ?? []; } = [];

        public List<Counted> PantryItems { get; init => field = value ?? []; } = [];

        public List<Counted> ManualShoppingItems { get; init => field = value ?? []; } = [];

        public List<Counted> Users { get; init => field = value ?? []; } = [];

        public List<Counted> GroceryStores { get; init => field = value ?? []; } = [];

        public List<Counted> PlanStyles { get; init => field = value ?? []; } = [];
    }

    /// <summary>A meal plan in a backup, counted by its meals and shopping items.</summary>
    private sealed class PlanOutline
    {
        public List<Counted> Meals { get; init => field = value ?? []; } = [];

        public List<Counted> ShoppingItems { get; init => field = value ?? []; } = [];
    }

    /// <summary>One record in a backup, counted but not read.</summary>
    private sealed class Counted;

    /// <summary>The backup file's JSON shape, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(BackupContents))]
    [JsonSerializable(typeof(BackupOutline))]
    private sealed partial class BackupJson : JsonSerializerContext;
}
