using ChefBot.Core.Accounts;
using ChefBot.Core.Backups;
using ChefBot.Core.Notifications;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using ChefBot.Core.Shopping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ChefBot.Infrastructure.Persistence;

/// <summary>The PostgreSQL database: recipes, weekly plans, the pantry and accounts.</summary>
public sealed class ChefBotDbContext(DbContextOptions<ChefBotDbContext> options) : DbContext(options)
{
    public DbSet<Recipe> Recipes => Set<Recipe>();

    public DbSet<MealPlan> MealPlans => Set<MealPlan>();

    public DbSet<PantryItem> PantryItems => Set<PantryItem>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AccessLink> AccessLinks => Set<AccessLink>();


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
        modelBuilder.Entity<Recipe>(recipe =>
        {
            recipe.HasIndex(r => r.ExternalId).IsUnique();
            recipe.HasMany(r => r.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeIngredient>().ToTable("RecipeIngredients");

        modelBuilder.Entity<MealPlan>(plan =>
        {
            plan.HasIndex(p => p.WeekStart).IsUnique();
            plan.HasMany(p => p.Meals).WithOne().HasForeignKey(m => m.MealPlanId).OnDelete(DeleteBehavior.Cascade);
            plan.HasMany(p => p.ShoppingItems).WithOne().HasForeignKey(i => i.MealPlanId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlannedMeal>(meal =>
        {
            meal.ToTable("PlannedMeals");
            meal.HasOne(m => m.Recipe).WithMany().HasForeignKey(m => m.RecipeId).OnDelete(DeleteBehavior.SetNull);
            meal.HasMany(m => m.CustomIngredients).WithOne().HasForeignKey(i => i.PlannedMealId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlannedMealIngredient>().ToTable("PlannedMealIngredients");
        modelBuilder.Entity<ShoppingItem>().ToTable("ShoppingItems");

        modelBuilder.Entity<PantryItem>().HasIndex(item => item.Key);

        modelBuilder.Entity<User>().HasIndex(user => user.Username).IsUnique();
        modelBuilder.Entity<AccessLink>().HasIndex(link => link.TokenHash).IsUnique();

        // Settings: one row each, read through SettingsStore.
        modelBuilder.Entity<SecuritySettings>().ToTable("SecuritySettings");
        modelBuilder.Entity<NotificationSettings>().ToTable("NotificationSettings");
        modelBuilder.Entity<BackupSettings>().ToTable("BackupSettings");

        UseKeysGeneratedInCode(modelBuilder);
    }

    /// <summary>
    /// Ids are created in code (see the entities' Id initializers). Telling EF so makes it insert
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
