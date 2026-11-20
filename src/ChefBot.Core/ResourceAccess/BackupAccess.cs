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
            Recipes = await db.Recipes.AsNoTracking().Include(recipe => recipe.Ingredients).ToListAsync(cancellationToken),
            MealPlans = await db.MealPlans.AsNoTracking()
                .Include(plan => plan.Meals).ThenInclude(meal => meal.CustomIngredients)
                .Include(plan => plan.ShoppingItems)
                .AsSplitQuery()
                .ToListAsync(cancellationToken),
            PantryItems = await db.PantryItems.AsNoTracking().ToListAsync(cancellationToken),
            Users = await db.Users.AsNoTracking().ToListAsync(cancellationToken),
            Security = await db.Set<SecuritySettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Notifications = await db.Set<NotificationSettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Backups = await db.Set<BackupSettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Mealie = await db.Set<MealieSettingsEntity>().AsNoTracking().ToListAsync(cancellationToken),
            Schedule = await db.Set<ScheduleStateEntity>().AsNoTracking().ToListAsync(cancellationToken),
            GroceryStores = await db.GroceryStores.AsNoTracking().ToListAsync(cancellationToken),
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

        // Children go with their parents: the database deletes them through its foreign keys.
        await db.AccessLinks.ExecuteDeleteAsync(cancellationToken);
        await db.MealPlans.ExecuteDeleteAsync(cancellationToken);
        await db.Recipes.ExecuteDeleteAsync(cancellationToken);
        await db.PantryItems.ExecuteDeleteAsync(cancellationToken);
        await db.Users.ExecuteDeleteAsync(cancellationToken);
        await db.Set<SecuritySettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<NotificationSettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<BackupSettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<MealieSettingsEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<ScheduleStateEntity>().ExecuteDeleteAsync(cancellationToken);
        await db.GroceryStores.ExecuteDeleteAsync(cancellationToken);

        db.ChangeTracker.Clear();
        db.AddRange(contents.Recipes);
        db.AddRange(contents.MealPlans);
        db.AddRange(contents.PantryItems);
        db.AddRange(contents.Users);
        db.AddRange(contents.Security);
        db.AddRange(contents.Notifications);
        db.AddRange(contents.Backups);
        db.AddRange(contents.Mealie);
        db.AddRange(contents.Schedule);
        db.AddRange(contents.GroceryStores);

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

        public List<MealPlanEntity> MealPlans { get; set; } = [];

        public List<PantryItemEntity> PantryItems { get; set; } = [];

        public List<UserEntity> Users { get; set; } = [];

        public List<SecuritySettingsEntity> Security { get; set; } = [];

        public List<NotificationSettingsEntity> Notifications { get; set; } = [];

        public List<BackupSettingsEntity> Backups { get; set; } = [];

        /// <summary>Empty in backups made before Mealie could be set up on the Settings tab.</summary>
        public List<MealieSettingsEntity> Mealie { get; set; } = [];

        /// <summary>Empty in backups made before the weekly schedule remembered what it staged.</summary>
        public List<ScheduleStateEntity> Schedule { get; set; } = [];

        /// <summary>Empty in backups made before stores could be chosen in the web page.</summary>
        public List<GroceryStoreEntity> GroceryStores { get; set; } = [];
    }

    /// <summary>The backup file's JSON shape, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(BackupContents))]
    private sealed partial class BackupJson : JsonSerializerContext;
}
