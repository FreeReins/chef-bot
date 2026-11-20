using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ChefBot.Core.Accounts;
using ChefBot.Core.Backups;
using ChefBot.Core.Notifications;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Infrastructure.Persistence;

/// <summary>
/// Backs the database up to compressed JSON files and restores from them. The files hold
/// ChefBot's own data rather than a PostgreSQL dump, so they don't depend on database tools
/// or versions. They include password hashes: treat them as private.
/// </summary>
public sealed class DatabaseBackupStore(ChefBotDbContext db, string folder) : IBackupStore
{
    private const string Pattern = "chefbot-backup-*.json.gz";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { LeaveOutRecipeCopies } },
    };

    public async Task<BackupFile> CreateAsync(string name, CancellationToken cancellationToken)
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
            Security = await db.Set<SecuritySettings>().AsNoTracking().ToListAsync(cancellationToken),
            Notifications = await db.Set<NotificationSettings>().AsNoTracking().ToListAsync(cancellationToken),
            Backups = await db.Set<BackupSettings>().AsNoTracking().ToListAsync(cancellationToken),
        };

        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        await using (var file = File.Create(path))
        await using (var zip = new GZipStream(file, CompressionLevel.Optimal))
        {
            await JsonSerializer.SerializeAsync(zip, contents, Json, cancellationToken);
        }

        return Describe(new FileInfo(path));
    }

    public IReadOnlyList<BackupFile> List() =>
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
            contents = await JsonSerializer.DeserializeAsync<BackupContents>(zip, Json, cancellationToken)
                ?? throw new InvalidDataException("The backup file is empty.");
        }

        // One transaction: either everything is replaced, or nothing changes.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Children go with their parents: the database deletes them through its foreign keys.
        await db.AccessLinks.ExecuteDeleteAsync(cancellationToken);
        await db.MealPlans.ExecuteDeleteAsync(cancellationToken);
        await db.Recipes.ExecuteDeleteAsync(cancellationToken);
        await db.PantryItems.ExecuteDeleteAsync(cancellationToken);
        await db.Users.ExecuteDeleteAsync(cancellationToken);
        await db.Set<SecuritySettings>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<NotificationSettings>().ExecuteDeleteAsync(cancellationToken);
        await db.Set<BackupSettings>().ExecuteDeleteAsync(cancellationToken);

        db.ChangeTracker.Clear();
        db.AddRange(contents.Recipes);
        db.AddRange(contents.MealPlans);
        db.AddRange(contents.PantryItems);
        db.AddRange(contents.Users);
        db.AddRange(contents.Security);
        db.AddRange(contents.Notifications);
        db.AddRange(contents.Backups);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static BackupFile Describe(FileInfo file) =>
        new(file.Name, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero), file.Length);

    /// <summary>
    /// A planned meal points at its recipe by id. Leaving the recipe object itself out keeps each
    /// recipe in the file once, so restoring never tries to insert the same recipe twice.
    /// </summary>
    private static void LeaveOutRecipeCopies(JsonTypeInfo type)
    {
        if (type.Type != typeof(PlannedMeal))
        {
            return;
        }

        for (var index = type.Properties.Count - 1; index >= 0; index--)
        {
            if (string.Equals(type.Properties[index].Name, nameof(PlannedMeal.Recipe), StringComparison.OrdinalIgnoreCase))
            {
                type.Properties.RemoveAt(index);
            }
        }
    }

    /// <summary>Everything a backup file holds.</summary>
    private sealed class BackupContents
    {
        public DateTimeOffset CreatedAt { get; set; }

        public List<Recipe> Recipes { get; set; } = [];

        public List<MealPlan> MealPlans { get; set; } = [];

        public List<PantryItem> PantryItems { get; set; } = [];

        public List<User> Users { get; set; } = [];

        public List<SecuritySettings> Security { get; set; } = [];

        public List<NotificationSettings> Notifications { get; set; } = [];

        public List<BackupSettings> Backups { get; set; } = [];
    }
}
