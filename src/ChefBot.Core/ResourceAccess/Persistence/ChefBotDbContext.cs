using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>The PostgreSQL database: recipes, weekly plans, the pantry, the hand-added shopping list, accounts, stores, plan styles, settings and the food history.</summary>
internal sealed class ChefBotDbContext(DbContextOptions<ChefBotDbContext> options) : DbContext(options)
{
    public DbSet<RecipeEntity> Recipes => Set<RecipeEntity>();

    public DbSet<RecipeImageDataEntity> RecipeImageData => Set<RecipeImageDataEntity>();

    public DbSet<MealPlanEntity> MealPlans => Set<MealPlanEntity>();

    public DbSet<PantryItemEntity> PantryItems => Set<PantryItemEntity>();

    public DbSet<ManualShoppingItemEntity> ManualShoppingItems => Set<ManualShoppingItemEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<AccessLinkEntity> AccessLinks => Set<AccessLinkEntity>();

    public DbSet<ApiTokenEntity> ApiTokens => Set<ApiTokenEntity>();

    public DbSet<GroceryStoreEntity> GroceryStores => Set<GroceryStoreEntity>();

    public DbSet<PlanStyleEntity> PlanStyles => Set<PlanStyleEntity>();

    public DbSet<ShoppingTargetEntity> ShoppingTargets => Set<ShoppingTargetEntity>();

    public DbSet<FoodMomentEntity> FoodMoments => Set<FoodMomentEntity>();

    public DbSet<RecipeFavouriteEntity> RecipeFavourites => Set<RecipeFavouriteEntity>();

    public DbSet<RecipeReviewEntity> RecipeReviews => Set<RecipeReviewEntity>();

    public DbSet<AchievementUnlockEntity> AchievementUnlocks => Set<AchievementUnlockEntity>();

    public DbSet<AiModelEntity> AiModels => Set<AiModelEntity>();

    public DbSet<AiRouteEntity> AiRoutes => Set<AiRouteEntity>();

    public DbSet<RecipeSuggestionEntity> RecipeSuggestions => Set<RecipeSuggestionEntity>();

    public DbSet<CookbookEntity> Cookbooks => Set<CookbookEntity>();

    public DbSet<InboxEntryEntity> InboxEntries => Set<InboxEntryEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 3);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table names are given explicitly so they don't follow the entity class names.
        modelBuilder.Entity<RecipeEntity>(recipe =>
        {
            recipe.ToTable("Recipes");
            recipe.HasMany(r => r.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
            recipe.HasMany(r => r.Images).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeIngredientEntity>().ToTable("RecipeIngredients");
        modelBuilder.Entity<CookbookEntity>().ToTable("Cookbooks");
        modelBuilder.Entity<RecipeImageEntity>().ToTable("RecipeImages");
        modelBuilder.Entity<RecipeImageDataEntity>(data =>
        {
            data.ToTable("RecipeImageData");
            data.HasKey(d => d.ImageId);
            data.HasOne<RecipeImageEntity>().WithOne().HasForeignKey<RecipeImageDataEntity>(d => d.ImageId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MealPlanEntity>(plan =>
        {
            plan.ToTable("MealPlans");
            plan.HasIndex(p => p.WeekStart).IsUnique();
            plan.HasMany(p => p.Meals).WithOne().HasForeignKey(m => m.MealPlanId).OnDelete(DeleteBehavior.Cascade);
            plan.HasMany(p => p.ShoppingItems).WithOne().HasForeignKey(i => i.MealPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlannedMealEntity>(meal =>
        {
            meal.ToTable("PlannedMeals");
            meal.HasOne(m => m.Recipe).WithMany().HasForeignKey(m => m.RecipeId).OnDelete(DeleteBehavior.SetNull);
            meal.HasMany(m => m.CustomIngredients).WithOne().HasForeignKey(i => i.PlannedMealId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlannedMealIngredientEntity>().ToTable("PlannedMealIngredients");
        modelBuilder.Entity<ShoppingItemEntity>(item =>
        {
            item.ToTable("ShoppingItems");
            item.Property(i => i.ExtraAmounts).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        });

        modelBuilder.Entity<PantryItemEntity>(item =>
        {
            item.ToTable("PantryItems");
            item.HasIndex(i => i.Key);
        });
        modelBuilder.Entity<ManualShoppingItemEntity>(item =>
        {
            item.ToTable("ManualShoppingItems");
            item.Property(i => i.ExtraAmounts).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        });

        modelBuilder.Entity<UserEntity>(user =>
        {
            user.ToTable("Users");
            user.HasIndex(u => u.Username).IsUnique();
        });
        modelBuilder.Entity<AccessLinkEntity>(link =>
        {
            link.ToTable("AccessLinks");
            link.HasIndex(l => l.TokenHash).IsUnique();
        });
        modelBuilder.Entity<ApiTokenEntity>(token =>
        {
            token.ToTable("ApiTokens");
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.UserId);
        });

        modelBuilder.Entity<GroceryStoreEntity>().ToTable("GroceryStores");
        modelBuilder.Entity<ShoppingTargetEntity>().ToTable("ShoppingTargets");
        modelBuilder.Entity<PlanStyleEntity>(style =>
        {
            style.ToTable("PlanStyles");
            style.Property(s => s.Rules).HasColumnType("jsonb");
        });

        modelBuilder.Entity<SecuritySettingsEntity>().ToTable("SecuritySettings");
        modelBuilder.Entity<NotificationSettingsEntity>().ToTable("NotificationSettings");
        modelBuilder.Entity<BackupSettingsEntity>().ToTable("BackupSettings");
        modelBuilder.Entity<ScheduleStateEntity>().ToTable("ScheduleState");
        modelBuilder.Entity<CalendarFeedEntity>().ToTable("CalendarFeed");
        modelBuilder.Entity<PlannerSettingsEntity>().ToTable("PlannerSettings");
        modelBuilder.Entity<AdvancedSettingsEntity>().ToTable("AdvancedSettings");
        modelBuilder.Entity<CalendarSettingsEntity>().ToTable("CalendarSettings");
        modelBuilder.Entity<FoodMomentEntity>(moment =>
        {
            moment.ToTable("FoodMoments");
            moment.HasIndex(m => m.OccurredOn);
            moment.HasIndex(m => m.MealId);
            moment.HasIndex(m => m.SourceId);
        });
        modelBuilder.Entity<RecipeFavouriteEntity>(favourite =>
        {
            favourite.ToTable("RecipeFavourites");
            favourite.HasIndex(f => new { f.RecipeId, f.UserId }).IsUnique();
            favourite.HasIndex(f => f.UserId);
        });
        modelBuilder.Entity<RecipeReviewEntity>(review =>
        {
            review.ToTable("RecipeReviews");
            review.HasIndex(r => r.RecipeId);
        });
        modelBuilder.Entity<AchievementUnlockEntity>(unlock =>
        {
            unlock.ToTable("AchievementUnlocks");
            unlock.HasIndex(u => new { u.Key, u.Tier }).IsUnique();
        });
        modelBuilder.Entity<AiModelEntity>().ToTable("AiModels");
        modelBuilder.Entity<AiRouteEntity>(route =>
        {
            route.ToTable("AiRoutes");
            route.HasKey(r => r.Task);
        });
        modelBuilder.Entity<RecipeSuggestionEntity>(suggestion =>
        {
            suggestion.ToTable("RecipeSuggestions");
            suggestion.HasIndex(s => s.RecipeId).IsUnique();
            suggestion.Property(s => s.Changes).HasColumnType("jsonb");
        });

        modelBuilder.Entity<InboxEntryEntity>(entry =>
        {
            entry.ToTable("InboxEntries");
            entry.HasIndex(e => new { e.UserId, e.CreatedAt });
            entry.HasIndex(e => new { e.UserId, e.SourceId }).IsUnique().HasFilter("\"SourceId\" IS NOT NULL");
        });

        StoreEnumsByName(modelBuilder);
        UseKeysGeneratedInCode(modelBuilder);
    }

    /// <summary>
    /// Stores every enum column by name, so the tables stay readable and survive reordering. Applies to enums added
    /// later too; <c>SchemaTests</c> checks it.
    /// </summary>
    private static void StoreEnumsByName(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            foreach (var property in entityType.GetProperties().ToList())
            {
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType).IsEnum)
                {
                    modelBuilder.Entity(entityType.ClrType).Property(property.Name).HasConversion<string>().HasMaxLength(20);
                }
            }
        }
    }

    /// <summary>
    /// Ids are created in code (see the data contracts' Id initializers). Telling EF so makes it insert
    /// children added to an already-saved parent, instead of assuming they exist and updating them.
    /// </summary>
    private static void UseKeysGeneratedInCode(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetKeys().SelectMany(key => key.Properties))
            {
                property.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}
