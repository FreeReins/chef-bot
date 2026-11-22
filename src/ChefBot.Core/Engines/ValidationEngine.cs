using System.Text.RegularExpressions;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Engines;

/// <summary>
/// Checks requests against ChefBot's rules before anything changes, and says in a plain sentence what is wrong.
/// Rules that depend on what is stored (does the plan exist, is the username taken, is setup done) read it through
/// the resource accessors; this is the only engine that does.
/// </summary>
internal sealed partial class ValidationEngine(
    IPlanAccess plans, IRecipeAccess recipes, IPantryAccess pantry, IShoppingListAccess shoppingList, IAccountAccess accounts, IBackupAccess backups)
{
    public const int MinimumPasswordLength = 8;

    /// <summary>The longest name an API token can have.</summary>
    public const int ApiTokenNameLength = 80;

    private const int MaxStores = 50;

    private const int MaxStoreName = 60;

    private const int MaxShoppingTargets = 10;

    private const int MaxPlanStyles = 20;

    private const int MaxPlanStyleName = 60;

    private const int MaxPlanRules = 10;

    /// <summary>The most snacks, sweets or drinks a style plans, and the most meals a rule counts.</summary>
    private const int MaxExtras = 7;

    /// <summary>The longest time a "ready within" rule allows: ten hours.</summary>
    private const int MaxRuleMinutes = 600;

    /// <summary>The largest photo ChefBot keeps, in bytes.</summary>
    public const int MaxImageBytes = 5 * 1024 * 1024;

    /// <summary>The most photos one recipe can have.</summary>
    public const int MaxImagesPerRecipe = 10;

    /// <summary>A week: no step of cooking a recipe takes longer.</summary>
    private const int MaxMinutes = 7 * 24 * 60;

    private static readonly Regex ValidUsername = new("^[a-z0-9._-]{3,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ValidBackupName = new(@"^chefbot-backup-\d{8}-\d{6}\.json\.gz$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<MealPlan> PlanAsync(Guid id, CancellationToken cancellationToken) =>
        await plans.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"There is no meal plan with id {id}.");

    public async Task<Recipe> RecipeAsync(Guid id, CancellationToken cancellationToken) =>
        await recipes.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"There is no recipe with id {id}.");

    public async Task<PantryItem> PantryItemAsync(Guid id, CancellationToken cancellationToken) =>
        await pantry.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"There is no pantry item with id {id}.");

    public async Task<ManualShoppingItem> ManualShoppingItemAsync(Guid id, CancellationToken cancellationToken) =>
        await shoppingList.FindAsync(id, cancellationToken)
        ?? throw new NotFoundException("That item is no longer on the shopping list. Reload the page to see the current list.");

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

    private static string Archived(Recipe recipe) => recipe.IsArchived ? " (archived)" : string.Empty;

    /// <summary>Whether two page addresses are the same page, ignoring case and a leading www. in the host, and a trailing slash.</summary>
    private static bool SamePage(string? first, string? second) =>
        Uri.TryCreate(first, UriKind.Absolute, out var a) && Uri.TryCreate(second, UriKind.Absolute, out var b)
        && string.Equals(Normal(a), Normal(b), StringComparison.Ordinal);

    private static string Normal(Uri url) =>
        $"{(url.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? url.Host[4..] : url.Host).ToLowerInvariant()}{url.AbsolutePath.TrimEnd('/')}{url.Query}";
}
