using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

// The tables, as EF Core sees them. Only the accessors use these; everything else gets the data contracts in
// ResourceAccess/Contracts. Backup files are these rows serialized as JSON, so renaming a property here changes
// the backup format: keep old names readable when you do.

internal sealed class RecipeEntity
{
    public Guid Id { get; set; }

    public RecipeOrigin Origin { get; set; }

    public string? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; }

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<RecipeIngredientEntity> Ingredients { get; set; } = [];
}

internal sealed class RecipeIngredientEntity
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public int Position { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;
}

internal sealed class MealPlanEntity
{
    public Guid Id { get; set; }

    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<PlannedMealEntity> Meals { get; set; } = [];

    public List<ShoppingItemEntity> ShoppingItems { get; set; } = [];
}

internal sealed class PlannedMealEntity
{
    public Guid Id { get; set; }

    public Guid MealPlanId { get; set; }

    public DateOnly? Date { get; set; }

    public MealSlot Slot { get; set; }

    public Guid? RecipeId { get; set; }

    /// <summary>Loaded with a plan. Never written through: a meal only stores <see cref="RecipeId"/>.</summary>
    public RecipeEntity? Recipe { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Portions { get; set; }

    public bool IsOptional { get; set; }

    public Guid? LeftoverOfMealId { get; set; }

    public MealStatus Status { get; set; }

    public List<PlannedMealIngredientEntity> CustomIngredients { get; set; } = [];
}

internal sealed class PlannedMealIngredientEntity
{
    public Guid Id { get; set; }

    public Guid PlannedMealId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}

internal sealed class ShoppingItemEntity
{
    public Guid Id { get; set; }

    public Guid MealPlanId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public string Store { get; set; } = string.Empty;

    public bool IsOptional { get; set; }

    public bool IsPurchased { get; set; }

    public DateOnly? PurchasedOn { get; set; }
}

internal sealed class PantryItemEntity
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateOnly PurchasedOn { get; set; }

    public DateOnly? ExpiresOn { get; set; }
}

internal sealed class UserEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int FailedSignIns { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public int SessionVersion { get; set; }
}

internal sealed class AccessLinkEntity
{
    public Guid Id { get; set; }

    public LinkPurpose Purpose { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}

// Settings: one row each, with Id 1.

internal sealed class SecuritySettingsEntity
{
    public int Id { get; set; } = 1;

    public int MaxFailedSignIns { get; set; }

    public int LockoutMinutes { get; set; }

    public bool RequireHttps { get; set; }
}

internal sealed class NotificationSettingsEntity
{
    public int Id { get; set; } = 1;

    public string WebhookUrl { get; set; } = string.Empty;

    public bool PlanReady { get; set; }

    public bool SlowCooker { get; set; }

    public bool MealsToday { get; set; }

    public bool ExpiringFood { get; set; }

    public bool Shopping { get; set; }

    public string DailyTime { get; set; } = string.Empty;

    public DateOnly? LastDailyRunOn { get; set; }
}

internal sealed class BackupSettingsEntity
{
    public int Id { get; set; } = 1;

    public bool Enabled { get; set; }

    public string Time { get; set; } = string.Empty;

    public int KeepCount { get; set; }

    public int KeepDays { get; set; }

    public DateOnly? LastRunOn { get; set; }

    public string LastResult { get; set; } = string.Empty;
}

internal sealed class MealieSettingsEntity
{
    public int Id { get; set; } = 1;

    public string Url { get; set; } = string.Empty;

    public string ApiToken { get; set; } = string.Empty;
}

internal sealed class ScheduleStateEntity
{
    public int Id { get; set; } = 1;

    public DateOnly? LastStagedWeek { get; set; }
}
