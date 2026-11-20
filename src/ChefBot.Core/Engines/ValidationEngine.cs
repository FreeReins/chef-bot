using System.Text.RegularExpressions;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>
/// Checks requests against ChefBot's rules before anything changes, and says in a plain sentence what is wrong.
/// Rules that depend on what is stored (does the plan exist, is the username taken, is setup done) read it through
/// the resource accessors; this is the only engine that does.
/// </summary>
internal sealed class ValidationEngine(IPlanAccess plans, IRecipeAccess recipes, IPantryAccess pantry, IAccountAccess accounts, IBackupAccess backups)
{
    public const int MinimumPasswordLength = 8;

    private static readonly Regex ValidUsername = new("^[a-z0-9._-]{3,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ValidBackupName = new(@"^chefbot-backup-\d{8}-\d{6}\.json\.gz$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // ---------- things that must exist ----------

    public async Task<MealPlan> PlanAsync(Guid id, CancellationToken cancellationToken) =>
        await plans.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"There is no meal plan with id {id}.");

    public async Task<Recipe> RecipeAsync(Guid id, CancellationToken cancellationToken) =>
        await recipes.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"There is no recipe with id {id}.");

    public async Task<PantryItem> PantryItemAsync(Guid id, CancellationToken cancellationToken) =>
        await pantry.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"There is no pantry item with id {id}.");

    public async Task<UserAccount> UserAsync(Guid id, CancellationToken cancellationToken) =>
        await accounts.FindUserAsync(id, cancellationToken) ?? throw new NotFoundException("That account no longer exists.");

    public async Task<AccessLink> LinkAsync(Guid id, CancellationToken cancellationToken) =>
        await accounts.FindLinkAsync(id, cancellationToken) ?? throw new NotFoundException("That link no longer exists.");

    public static PlannedMeal Meal(MealPlan plan, Guid mealId) =>
        plan.Meals.FirstOrDefault(meal => meal.Id == mealId)
        ?? throw new NotFoundException($"This plan has no meal with id {mealId}.");

    public static ShoppingItem ShoppingItem(MealPlan plan, Guid itemId) =>
        plan.ShoppingItems.FirstOrDefault(item => item.Id == itemId)
        ?? throw new NotFoundException($"This plan has no shopping item with id {itemId}.");

    /// <summary>A backup's name, once it is known to be a real backup file and not an attempt to reach another file.</summary>
    public string ExistingBackup(string name) =>
        ValidBackupName.IsMatch(name ?? string.Empty) && backups.List().Any(file => file.Name == name)
            ? name!
            : throw new NotFoundException("There is no backup with that name.");

    // ---------- plans ----------

    /// <summary>Checks a week can be staged, and returns the plan it would replace, if any.</summary>
    public async Task<MealPlan?> WeekToStageAsync(DateOnly weekStart, bool replace, CancellationToken cancellationToken)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Sunday)
        {
            throw new RuleViolationException("A plan's week must start on a Sunday.");
        }

        var existing = await plans.FindByWeekAsync(weekStart, cancellationToken);
        if (existing is { Status: PlanStatus.Confirmed } && !replace)
        {
            throw new ConflictException($"The week of {weekStart:yyyy-MM-dd} is already confirmed. Pass replace to start it over.");
        }

        return existing;
    }

    public static void CanBePlanned(Recipe recipe)
    {
        if (recipe.IsArchived)
        {
            throw new RuleViolationException($"{recipe.Name} has been archived and can't be planned.");
        }
    }

    public static void ReplaceRequest(ReplaceMealRequest request)
    {
        if (request is not ({ RecipeId: not null, Custom: null } or { RecipeId: null, Custom: not null }))
        {
            throw new RuleViolationException("Send either recipeId or custom, but not both.");
        }
    }

    public static void CustomMeal(string? title, IReadOnlyCollection<PlannedMealIngredient> ingredients)
    {
        if (string.IsNullOrWhiteSpace(title) || ingredients.Count == 0)
        {
            throw new RuleViolationException("A custom meal needs a title and at least one ingredient.");
        }
    }

    /// <summary>Purchases are recorded against the working plan only.</summary>
    public static void CanShopFor(MealPlan plan)
    {
        if (plan.Status != PlanStatus.Confirmed)
        {
            throw new ConflictException("Confirm the staged plan before recording purchases against it.");
        }
    }

    public static void NotYetPurchased(ShoppingItem item)
    {
        if (item.IsPurchased)
        {
            throw new ConflictException($"{item.DisplayName} is already marked as purchased.");
        }
    }

    // ---------- recipes ----------

    public static void Recipe(RecipeText recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            throw new RuleViolationException("A recipe needs a name.");
        }

        if (recipe.Servings is <= 0m)
        {
            throw new RuleViolationException("Servings must be more than zero.");
        }

        if (!(recipe.Ingredients ?? []).Any(line => !string.IsNullOrWhiteSpace(line)))
        {
            throw new RuleViolationException("A recipe needs at least one ingredient.");
        }
    }

    /// <summary>The address and token to save for Mealie. An empty address turns Mealie off; an empty token keeps the saved one.</summary>
    public static (string Url, string ApiToken) MealieConnection(string? url, string? apiToken, string savedToken)
    {
        var address = url?.Trim() ?? string.Empty;
        if (address.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (!WebAddress.IsHttp(address))
        {
            throw new RuleViolationException("Enter Mealie's full address, starting with http:// or https://.");
        }

        var token = string.IsNullOrWhiteSpace(apiToken) ? savedToken : apiToken.Trim();
        if (token.Length == 0)
        {
            throw new RuleViolationException("Enter an API token. Create one in Mealie under your user profile, API Tokens.");
        }

        return (address, token);
    }

    public static void MealieIsSetUp(MealieSettings settings)
    {
        if (!settings.IsConfigured)
        {
            throw new RuleViolationException("Mealie isn't set up. An admin can enter its address and an API token on the Settings tab.");
        }
    }

    // ---------- pantry ----------

    public static void PantryItemName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RuleViolationException("A pantry item needs a name.");
        }
    }

    public static void Stock(decimal? quantity, DateOnly purchasedOn, DateOnly? expiresOn)
    {
        if (quantity is <= 0m)
        {
            throw new RuleViolationException("A quantity must be more than zero.");
        }

        if (expiresOn < purchasedOn)
        {
            throw new RuleViolationException("The expiration date can't be before the purchase date.");
        }
    }

    public static void QuantityLeft(decimal? quantity)
    {
        if (quantity is < 0m)
        {
            throw new RuleViolationException("A quantity can't be negative.");
        }
    }

    // ---------- accounts ----------

    public async Task SetupIsOpenAsync(CancellationToken cancellationToken)
    {
        if (await accounts.AnyUsersAsync(cancellationToken))
        {
            throw new ConflictException("ChefBot is already set up. Sign in, or ask an admin for an invite link.");
        }
    }

    /// <summary>Checks the details for a new account and returns its normalized username.</summary>
    public async Task<string> NewAccountAsync(NewAccount account, CancellationToken cancellationToken)
    {
        var username = Usernames.Normalize(account.Username);
        if (!ValidUsername.IsMatch(username))
        {
            throw new RuleViolationException("A username needs 3 to 32 characters: letters, numbers, dots, dashes or underscores.");
        }

        Password(account.Password);
        if (await accounts.FindUserByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new ConflictException($"The username {username} is already taken.");
        }

        return username;
    }

    public static string Password(string? password) =>
        (password ?? string.Empty).Length >= MinimumPasswordLength
            ? password!
            : throw new RuleViolationException($"A password needs at least {MinimumPasswordLength} characters.");

    public static void CanRemove(Guid id, Guid actingUserId)
    {
        if (id == actingUserId)
        {
            throw new RuleViolationException("You can't remove your own account.");
        }
    }

    // ---------- settings ----------

    public static void SignInRules(SecurityRequest request)
    {
        if (request.MaxFailedSignIns is < 0 or > 100)
        {
            throw new RuleViolationException("Failed attempts must be between 0 and 100. Zero turns lockout off.");
        }

        if (request.LockoutMinutes is < 1 or > 1440)
        {
            throw new RuleViolationException("The lockout must last between 1 minute and 24 hours.");
        }
    }

    /// <summary>The webhook address to save: empty, or a full http or https address.</summary>
    public static string Webhook(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        return trimmed.Length == 0 || WebAddress.IsHttp(trimmed)
            ? trimmed
            : throw new RuleViolationException("The webhook must be a full http or https address.");
    }

    public static void BackupLimits(int keepCount, int keepDays)
    {
        if (keepCount is < 0 or > 1000 || keepDays is < 0 or > 3650)
        {
            throw new RuleViolationException("Keep between 0 and 1000 backups, for between 0 and 3650 days. Zero means no limit.");
        }
    }

    /// <summary>A time of day in its saved form, HH:mm.</summary>
    public static string TimeOfDay(string? time) => DailyTime.Format(DailyTime.Parse(time));
}
