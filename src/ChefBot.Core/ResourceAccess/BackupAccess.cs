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
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        await using (var file = File.Create(path))
        await using (var zip = new GZipStream(file, CompressionLevel.Optimal))
        await using (var writer = new Utf8JsonWriter(zip))
        {
            writer.WriteStartObject();
            writer.WriteString("createdAt", DateTimeOffset.UtcNow);
            foreach (var table in Tables)
            {
                // One table's rows in memory at a time.
                await table.WriteAsync(db, writer, cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }

            writer.WriteEndObject();
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
        List<BackupRows> restored;
        await using (var file = File.OpenRead(Path.Combine(folder, name)))
        await using (var zip = new GZipStream(file, CompressionMode.Decompress))
        {
            using var backup = await JsonDocument.ParseAsync(zip, cancellationToken: cancellationToken);
            if (backup.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The backup file is empty.");
            }

            restored = Tables.Select(table => table.Read(backup.RootElement)).ToList();
        }

        // One transaction: either everything is replaced, or nothing changes.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var rows in restored)
        {
            await rows.BeforeRestoreAsync(db, cancellationToken);
        }

        // These aren't backed up, and point at users. Access links are made again. API tokens aren't backed up, so
        // a token revoked since the backup can't come back: like everyone signing in again, assistants need new
        // tokens after a restore.
        await db.AccessLinks.ExecuteDeleteAsync(cancellationToken);
        await db.ApiTokens.ExecuteDeleteAsync(cancellationToken);
        foreach (var table in Tables.Reverse())
        {
            await table.ClearAsync(db, cancellationToken);
        }

        db.ChangeTracker.Clear();
        foreach (var rows in restored)
        {
            rows.AddTo(db);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static StoredBackup Describe(FileInfo file) =>
        new(file.Name, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero), file.Length);

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
    [JsonSerializable(typeof(List<RecipeEntity>))]
    [JsonSerializable(typeof(List<RecipeImageDataEntity>))]
    [JsonSerializable(typeof(List<MealPlanEntity>))]
    [JsonSerializable(typeof(List<PantryItemEntity>))]
    [JsonSerializable(typeof(List<ManualShoppingItemEntity>))]
    [JsonSerializable(typeof(List<UserEntity>))]
    [JsonSerializable(typeof(List<SecuritySettingsEntity>))]
    [JsonSerializable(typeof(List<NotificationSettingsEntity>))]
    [JsonSerializable(typeof(List<BackupSettingsEntity>))]
    [JsonSerializable(typeof(List<ScheduleStateEntity>))]
    [JsonSerializable(typeof(List<GroceryStoreEntity>))]
    [JsonSerializable(typeof(List<PlanStyleEntity>))]
    [JsonSerializable(typeof(List<ShoppingTargetEntity>))]
    [JsonSerializable(typeof(List<CalendarFeedEntity>))]
    [JsonSerializable(typeof(List<FoodMomentEntity>))]
    [JsonSerializable(typeof(List<RecipeFavouriteEntity>))]
    [JsonSerializable(typeof(List<RecipeReviewEntity>))]
    [JsonSerializable(typeof(List<PlannerSettingsEntity>))]
    [JsonSerializable(typeof(List<AdvancedSettingsEntity>))]
    [JsonSerializable(typeof(List<CalendarSettingsEntity>))]
    [JsonSerializable(typeof(BackupOutline))]
    private sealed partial class BackupJson : JsonSerializerContext;
}
