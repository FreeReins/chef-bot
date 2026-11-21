using ChefBot.Core.Managers.Contracts.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>The PostgreSQL database: recipes, weekly plans, the pantry, the hand-added shopping list, accounts, stores, plan styles and settings.</summary>
internal sealed class ChefBotDbContext(DbContextOptions<ChefBotDbContext> options) : DbContext(options)
{
    public DbSet<RecipeEntity> Recipes => Set<RecipeEntity>();

    public DbSet<RecipeImageDataEntity> RecipeImageData => Set<RecipeImageDataEntity>();

    public DbSet<MealPlanEntity> MealPlans => Set<MealPlanEntity>();

    public DbSet<PantryItemEntity> PantryItems => Set<PantryItemEntity>();

    public DbSet<ManualShoppingItemEntity> ManualShoppingItems => Set<ManualShoppingItemEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<AccessLinkEntity> AccessLinks => Set<AccessLinkEntity>();

    public DbSet<GroceryStoreEntity> GroceryStores => Set<GroceryStoreEntity>();

    public DbSet<PlanStyleEntity> PlanStyles => Set<PlanStyleEntity>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored by name so the tables stay readable and survive reordering.
        configurationBuilder.Properties<RecipeCourse>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<RecipeOrigin>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<MealSlot>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<MealStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<PlanStatus>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<LinkPurpose>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<decimal>().HavePrecision(12, 3);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Table names are given explicitly so they don't follow the entity class names.
        modelBuilder.Entity<RecipeEntity>(recipe =>
        {
            recipe.ToTable("Recipes");
            recipe.HasIndex(r => r.ExternalId).IsUnique();
            recipe.HasMany(r => r.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
            recipe.HasMany(r => r.Images).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeIngredientEntity>().ToTable("RecipeIngredients");
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
        modelBuilder.Entity<ShoppingItemEntity>().ToTable("ShoppingItems");

        modelBuilder.Entity<PantryItemEntity>(item =>
        {
            item.ToTable("PantryItems");
            item.HasIndex(i => i.Key);
        });
        modelBuilder.Entity<ManualShoppingItemEntity>().ToTable("ManualShoppingItems");

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

        modelBuilder.Entity<GroceryStoreEntity>().ToTable("GroceryStores");
        modelBuilder.Entity<PlanStyleEntity>(style =>
        {
            style.ToTable("PlanStyles");
            style.Property(s => s.Rules).HasColumnType("jsonb");
        });

        modelBuilder.Entity<SecuritySettingsEntity>().ToTable("SecuritySettings");
        modelBuilder.Entity<NotificationSettingsEntity>().ToTable("NotificationSettings");
        modelBuilder.Entity<BackupSettingsEntity>().ToTable("BackupSettings");
        modelBuilder.Entity<MealieSettingsEntity>().ToTable("MealieSettings");
        modelBuilder.Entity<ScheduleStateEntity>().ToTable("ScheduleState");

        UseKeysGeneratedInCode(modelBuilder);
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
