using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts;

// The plain data contracts that resource accessors hand to managers and engines, and take back to save.
// They are copies: changing one changes nothing until it is passed back to an accessor's Save method.
// The EF Core entities behind them are private to ResourceAccess/Persistence.

/// <summary>A recipe stored in ChefBot's own database.</summary>
internal sealed class Recipe
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public RecipeOrigin Origin { get; set; }

    /// <summary>The id in the system it was imported from, if any.</summary>
    public string? ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Servings { get; set; } = 4;

    public RecipeCourse Course { get; set; }

    public bool IsSlowCooker { get; set; }

    /// <summary>Archived recipes stay on past plans but are never planned again.</summary>
    public bool IsArchived { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<RecipeIngredient> Ingredients { get; set; } = [];
}

/// <summary>One ingredient line of a recipe.</summary>
internal sealed class RecipeIngredient : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The line's place in the recipe.</summary>
    public int Position { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The amount for the recipe as written, or null when the recipe doesn't say.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;
}

/// <summary>A week of meals, snacks and sweets, with the shopping list that goes with it.</summary>
internal sealed class MealPlan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The Sunday the week starts on. There is one plan per week.</summary>
    public DateOnly WeekStart { get; set; }

    public PlanStatus Status { get; set; } = PlanStatus.Staged;

    public DateTimeOffset CreatedAt { get; set; }

    public List<PlannedMeal> Meals { get; set; } = [];

    public List<ShoppingItem> ShoppingItems { get; set; } = [];
}

/// <summary>One entry on a weekly plan: a recipe, leftovers, or a free-form meal built from named ingredients.</summary>
internal sealed class PlannedMeal
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The suggested day. Snacks and sweets have none: make them whenever suits.</summary>
    public DateOnly? Date { get; set; }

    public MealSlot Slot { get; set; }

    public Guid? RecipeId { get; set; }

    /// <summary>The recipe as it was loaded with the plan. Saving a plan only stores <see cref="RecipeId"/>.</summary>
    public Recipe? Recipe { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Portions to cook. Zero means "make the recipe once, as written".</summary>
    public int Portions { get; set; }

    /// <summary>Optional meals are planned but their groceries are marked as skippable.</summary>
    public bool IsOptional { get; set; }

    /// <summary>The dinner this lunch is leftovers of, if it is leftovers.</summary>
    public Guid? LeftoverOfMealId { get; set; }

    public MealStatus Status { get; set; } = MealStatus.Planned;

    /// <summary>Ingredients of a free-form meal. Empty when the meal follows a recipe.</summary>
    public List<PlannedMealIngredient> CustomIngredients { get; set; } = [];

    public bool IsLeftover => LeftoverOfMealId is not null;

    /// <summary>What goes into this meal, whichever way it is defined.</summary>
    public IReadOnlyList<IIngredientRef> Ingredients => Recipe is not null ? Recipe.Ingredients : CustomIngredients;
}

/// <summary>An ingredient of a free-form meal. Amounts aren't tracked for these.</summary>
internal sealed class PlannedMealIngredient : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>One line of a plan's shopping list.</summary>
internal sealed class ShoppingItem : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How much to buy, or null when the recipes don't say.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    /// <summary>A first guess at where to buy it.</summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>True when only optional meals need it.</summary>
    public bool IsOptional { get; set; }

    public bool IsPurchased { get; set; }

    public DateOnly? PurchasedOn { get; set; }
}

/// <summary>Something in the kitchen: what it is, how much, when it was bought and when it expires.</summary>
internal sealed class PantryItem : IIngredientRef
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Key { get; set; } = string.Empty;

    public string? CatalogName { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How much is left, or null when the amount isn't tracked.</summary>
    public decimal? Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public DateOnly PurchasedOn { get; set; }

    /// <summary>The expiration date, estimated from the catalog's shelf life unless one was entered.</summary>
    public DateOnly? ExpiresOn { get; set; }
}

/// <summary>Someone who can sign in. Everyone shares the same recipes, plans and pantry.</summary>
internal sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The sign-in name: lower-case and unique.</summary>
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>A salted hash. The password itself is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Admins can invite and remove people. The account created during setup is an admin.</summary>
    public bool IsAdmin { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Wrong passwords in a row since the last successful sign-in or lockout.</summary>
    public int FailedSignIns { get; set; }

    /// <summary>Sign-in is refused until this time, after too many wrong passwords.</summary>
    public DateTimeOffset? LockedUntil { get; set; }

    /// <summary>Goes up whenever the password changes, which signs every older session out.</summary>
    public int SessionVersion { get; set; }
}

/// <summary>A single-use, expiring link: an invitation to join, or a password reset for one account.</summary>
internal sealed class AccessLink
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public LinkPurpose Purpose { get; set; }

    /// <summary>A hash of the secret in the link, so a copy of the database can't be used to get in.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>The account a password reset is for. Null for invites.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The admin who created it. Null when it was created by the recovery setting at startup.</summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>Sign-in rules an admin can change while ChefBot is running.</summary>
internal sealed class SecuritySettings
{
    /// <summary>Wrong passwords in a row before an account is locked. Zero turns lockout off.</summary>
    public int MaxFailedSignIns { get; set; } = 5;

    /// <summary>How long a locked account stays locked.</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Send anyone arriving over plain HTTP to the HTTPS address.</summary>
    public bool RequireHttps { get; set; }
}

/// <summary>Where notifications go and which ones are wanted.</summary>
internal sealed class NotificationSettings
{
    /// <summary>A URL that accepts a JSON POST, such as a Home Assistant webhook. Empty turns notifications off.</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    public bool PlanReady { get; set; } = true;

    public bool SlowCooker { get; set; } = true;

    public bool MealsToday { get; set; }

    public bool ExpiringFood { get; set; } = true;

    public bool Shopping { get; set; } = true;

    /// <summary>Local time the daily reminders are sent, as HH:mm.</summary>
    public string DailyTime { get; set; } = "07:30";

    /// <summary>The last day the daily reminders ran, so they go out once a day.</summary>
    public DateOnly? LastDailyRunOn { get; set; }
}

/// <summary>When backups run and how many are kept.</summary>
internal sealed class BackupSettings
{
    public bool Enabled { get; set; } = true;

    /// <summary>Local time the daily backup runs, as HH:mm.</summary>
    public string Time { get; set; } = "03:00";

    /// <summary>Keep this many of the newest backups. Zero keeps any number.</summary>
    public int KeepCount { get; set; } = 14;

    /// <summary>Delete backups older than this many days. Zero keeps them whatever their age.</summary>
    public int KeepDays { get; set; }

    /// <summary>The last day the scheduled backup ran, so it runs once a day.</summary>
    public DateOnly? LastRunOn { get; set; }

    /// <summary>How the last backup went, in a sentence.</summary>
    public string LastResult { get; set; } = string.Empty;
}

/// <summary>How to reach Mealie. Empty means ChefBot runs without Mealie.</summary>
internal sealed class MealieSettings
{
    /// <summary>The base address, for example http://192.168.1.20:9000.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>An API token created in the Mealie user profile. Never sent back to the page.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public bool IsConfigured => WebAddress.IsHttp(Url) && ApiToken.Length > 0;
}

/// <summary>What the weekly schedule has done, so it stages each week only once.</summary>
internal sealed class ScheduleState
{
    /// <summary>The last week the schedule staged, or tried to stage.</summary>
    public DateOnly? LastStagedWeek { get; set; }
}

/// <summary>A backup file on disk.</summary>
internal sealed record StoredBackup(string Name, DateTimeOffset CreatedAt, long SizeBytes);

/// <summary>One ingredient row as an outside source describes it.</summary>
/// <param name="FoodName">The parsed food, when the source has one.</param>
/// <param name="Quantity">The parsed amount, when the source has one.</param>
/// <param name="Unit">The parsed unit, when the source has one.</param>
/// <param name="Text">The free-text line, used when the row was never parsed into a food.</param>
internal sealed record SourceIngredient(string? FoodName, decimal? Quantity, string? Unit, string? Text);

/// <summary>A recipe as an outside source describes it.</summary>
internal sealed record SourceRecipe(
    string ExternalId,
    string Name,
    decimal? Servings,
    IReadOnlyList<string> Labels,
    IReadOnlyList<SourceIngredient> Ingredients);

/// <summary>Everything read from a source, and how many recipes couldn't be read.</summary>
internal sealed record RecipeSourceResult(IReadOnlyList<SourceRecipe> Recipes, int Skipped);

/// <summary>
/// A recipe written the way someone would type it in: ingredients are lines such as "2 cups kale". Sample recipes,
/// recipe files and the recipe form all arrive in this shape.
/// </summary>
internal sealed record RecipeText(
    string Name,
    RecipeCourse Course,
    IReadOnlyList<string> Ingredients,
    bool IsSlowCooker = false,
    decimal? Servings = null);

/// <summary>The kinds of notification ChefBot sends. The value is the <c>type</c> field of the webhook payload.</summary>
internal static class NotificationTypes
{
    /// <summary>A new plan was staged and is waiting for review.</summary>
    public const string PlanReady = "plan_ready";

    /// <summary>Tonight's dinner is a slow cooker meal that needs starting.</summary>
    public const string SlowCooker = "slow_cooker";

    /// <summary>What is planned for today.</summary>
    public const string MealsToday = "meals_today";

    /// <summary>Food in the pantry is about to expire.</summary>
    public const string ExpiringFood = "expiring_food";

    /// <summary>The week is about to start and the plan is unconfirmed or has groceries left to buy.</summary>
    public const string Shopping = "shopping";

    /// <summary>Sent from the Settings tab to check the webhook works.</summary>
    public const string Test = "test";
}

/// <summary>Something worth telling the household.</summary>
/// <param name="Type">One of <see cref="NotificationTypes"/>.</param>
/// <param name="Title">A short heading.</param>
/// <param name="Message">A ready-to-show sentence or two.</param>
/// <param name="Data">The facts behind the message, so the receiver can word its own.</param>
internal sealed record Notification(string Type, string Title, string Message, IReadOnlyDictionary<string, object?> Data);

/// <summary>Whether a notification reached the webhook.</summary>
internal sealed record Delivery(bool Delivered, string? Problem = null);
