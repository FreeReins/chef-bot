using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <content>The tables a backup holds, and how each is written, read back and cleared.</content>
internal sealed partial class BackupAccess
{
    /// <summary>
    /// Every table a backup holds, parents before children. Each one is a property of the backup file's JSON,
    /// under the name given, so names never change. A new table is one line here and its
    /// <c>[JsonSerializable(typeof(List&lt;...&gt;))]</c> on <see cref="BackupJson"/>. Restoring clears the
    /// tables in reverse order. Rows reached through <c>Include</c> are written inside their parent.
    /// </summary>
    internal static readonly IReadOnlyList<BackupTable> Tables =
    [
        new BackupTable<RecipeEntity>("recipes", BackupJson.Default.ListRecipeEntity,
            rows => rows.Include(recipe => recipe.Ingredients).Include(recipe => recipe.Images).AsSplitQuery()),

        // The recipe photos' bytes. Empty in backups made before ChefBot kept photos.
        new BackupTable<RecipeImageDataEntity>("recipeImageData", BackupJson.Default.ListRecipeImageDataEntity),

        new BackupTable<MealPlanEntity>("mealPlans", BackupJson.Default.ListMealPlanEntity,
            rows => rows.Include(plan => plan.Meals).ThenInclude(meal => meal.CustomIngredients).Include(plan => plan.ShoppingItems).AsSplitQuery(),
            prepare: DropMealRecipes),

        new BackupTable<PantryItemEntity>("pantryItems", BackupJson.Default.ListPantryItemEntity),

        // Empty in backups made before things could be added to the shopping list by hand.
        new BackupTable<ManualShoppingItemEntity>("manualShoppingItems", BackupJson.Default.ListManualShoppingItemEntity),

        new BackupTable<UserEntity>("users", BackupJson.Default.ListUserEntity, beforeRestore: EndSessionsAsync),
        new BackupTable<SecuritySettingsEntity>("security", BackupJson.Default.ListSecuritySettingsEntity),
        new BackupTable<NotificationSettingsEntity>("notifications", BackupJson.Default.ListNotificationSettingsEntity),
        new BackupTable<BackupSettingsEntity>("backups", BackupJson.Default.ListBackupSettingsEntity),

        // Empty in backups made before the weekly schedule remembered what it staged.
        new BackupTable<ScheduleStateEntity>("schedule", BackupJson.Default.ListScheduleStateEntity),

        // Empty in backups made before stores could be chosen in the web page.
        new BackupTable<GroceryStoreEntity>("groceryStores", BackupJson.Default.ListGroceryStoreEntity),

        // Empty in backups made before households could keep their own plan styles.
        new BackupTable<PlanStyleEntity>("planStyles", BackupJson.Default.ListPlanStyleEntity),

        // The to-do apps, with their access tokens. Empty in backups made before the shopping list could be sent to one.
        new BackupTable<ShoppingTargetEntity>("shoppingTargets", BackupJson.Default.ListShoppingTargetEntity),

        // The calendar feed's secret. Empty in backups made before the meal plan had a calendar feed.
        new BackupTable<CalendarFeedEntity>("calendarFeed", BackupJson.Default.ListCalendarFeedEntity),

        // The food history. Empty in backups made before ChefBot kept one.
        new BackupTable<FoodMomentEntity>("foodMoments", BackupJson.Default.ListFoodMomentEntity),

        // Who has which recipe as a favourite. Empty in backups made before favourites.
        new BackupTable<RecipeFavouriteEntity>("recipeFavourites", BackupJson.Default.ListRecipeFavouriteEntity),

        // Everyone's ratings and notes on recipes. Empty in backups made before reviews.
        new BackupTable<RecipeReviewEntity>("recipeReviews", BackupJson.Default.ListRecipeReviewEntity),
    ];

    /// <summary>A planned meal points at its recipe by id; the recipe itself is written once, under recipes.
    /// Older backups wrote each meal's recipe as well.</summary>
    private static void DropMealRecipes(List<MealPlanEntity> plans)
    {
        foreach (var meal in plans.SelectMany(plan => plan.Meals))
        {
            meal.Recipe = null;
        }
    }

    /// <summary>
    /// Everyone signs in again after a restore. Passwords go back to the backup's, and so would the session
    /// versions, which would bring back sessions that a password change since then had ended (a stolen cookie,
    /// say). So each account's version moves past both its current one and the backup's, which no cookie
    /// carries. An account removed since the backup has no current version; its sessions ended when it was
    /// removed, unless its password also changed between the backup and the removal.
    /// </summary>
    private static async Task EndSessionsAsync(ChefBotDbContext db, List<UserEntity> users, CancellationToken cancellationToken)
    {
        var currentVersions = await db.Users.AsNoTracking().ToDictionaryAsync(user => user.Id, user => user.SessionVersion, cancellationToken);
        foreach (var user in users)
        {
            user.SessionVersion = Math.Max(user.SessionVersion, currentVersions.GetValueOrDefault(user.Id)) + 1;
        }
    }

    /// <summary>One table in a backup file.</summary>
    internal abstract class BackupTable
    {
        /// <summary>The table's property in the backup file's JSON.</summary>
        public abstract string Name { get; }

        /// <summary>The stored entity the table's rows are.</summary>
        public abstract Type EntityType { get; }

        /// <summary>Writes the table's rows as the value of its property.</summary>
        public abstract Task WriteAsync(ChefBotDbContext db, Utf8JsonWriter writer, CancellationToken cancellationToken);

        /// <summary>Reads the table's rows from a backup file; none when an older backup lacks the table.</summary>
        public abstract BackupRows Read(JsonElement backup);

        /// <summary>Deletes every row; their children go through the database's foreign keys.</summary>
        public abstract Task ClearAsync(ChefBotDbContext db, CancellationToken cancellationToken);
    }

    /// <summary>A table's rows read from a backup file, waiting to be restored.</summary>
    internal abstract class BackupRows
    {
        /// <summary>Runs before any table is cleared, while the current rows can still be read.</summary>
        public abstract Task BeforeRestoreAsync(ChefBotDbContext db, CancellationToken cancellationToken);

        /// <summary>Adds the rows to the context, to be inserted on save.</summary>
        public abstract void AddTo(ChefBotDbContext db);
    }

    /// <summary>A table of <typeparamref name="TEntity"/> rows.</summary>
    /// <param name="name">The table's property in the backup file's JSON.</param>
    /// <param name="json">How a list of its rows is written, made at build time.</param>
    /// <param name="query">Includes the rows' children, which are written inside them.</param>
    /// <param name="prepare">Tidies rows just read from the database or from a backup file.</param>
    /// <param name="beforeRestore">Adjusts the backup's rows from the current ones, before they are cleared.</param>
    private sealed class BackupTable<TEntity>(
        string name,
        JsonTypeInfo<List<TEntity>> json,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? query = null,
        Action<List<TEntity>>? prepare = null,
        Func<ChefBotDbContext, List<TEntity>, CancellationToken, Task>? beforeRestore = null) : BackupTable
        where TEntity : class
    {
        public override string Name => name;

        public override Type EntityType => typeof(TEntity);

        public override async Task WriteAsync(ChefBotDbContext db, Utf8JsonWriter writer, CancellationToken cancellationToken)
        {
            var rows = db.Set<TEntity>().AsNoTracking();
            var list = await (query?.Invoke(rows) ?? rows).ToListAsync(cancellationToken);
            prepare?.Invoke(list);
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, list, json);
        }

        public override BackupRows Read(JsonElement backup)
        {
            var list = backup.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
                ? value.Deserialize(json) ?? []
                : [];
            prepare?.Invoke(list);
            return new Rows(list, beforeRestore);
        }

        public override Task ClearAsync(ChefBotDbContext db, CancellationToken cancellationToken) =>
            db.Set<TEntity>().ExecuteDeleteAsync(cancellationToken);

        private sealed class Rows(List<TEntity> list, Func<ChefBotDbContext, List<TEntity>, CancellationToken, Task>? beforeRestore) : BackupRows
        {
            public override Task BeforeRestoreAsync(ChefBotDbContext db, CancellationToken cancellationToken) =>
                beforeRestore?.Invoke(db, list, cancellationToken) ?? Task.CompletedTask;

            public override void AddTo(ChefBotDbContext db) => db.AddRange(list);
        }
    }
}
