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
internal sealed class ValidationEngine(
    IPlanAccess plans, IRecipeAccess recipes, IPantryAccess pantry, IShoppingListAccess shoppingList, IAccountAccess accounts, IBackupAccess backups)
{
    public const int MinimumPasswordLength = 8;

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

    // ---------- things that must exist ----------

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

        foreach (var (minutes, label) in new[] { (recipe.PrepMinutes, "Prep time"), (recipe.CookMinutes, "Cook time"), (recipe.TotalMinutes, "Total time") })
        {
            if (minutes is < 0 or > MaxMinutes)
            {
                throw new RuleViolationException($"{label} must be between 0 and {MaxMinutes} minutes (a week).");
            }
        }

        if (!string.IsNullOrWhiteSpace(recipe.SourceUrl) && !WebAddress.IsHttp(recipe.SourceUrl.Trim()))
        {
            throw new RuleViolationException("The source link must be a full web address starting with http:// or https://.");
        }
    }

    /// <summary>
    /// The address of a recipe page to import. An address typed without <c>https://</c>, such as
    /// <c>example.com/soup</c>, gets it added.
    /// </summary>
    public static Uri RecipePageAddress(string? url)
    {
        var trimmed = url?.Trim() ?? string.Empty;
        if (trimmed.Length > 0 && !trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed;
        }

        return WebAddress.IsHttp(trimmed) && Uri.TryCreate(trimmed, UriKind.Absolute, out var address) && address.Host.Contains('.')
            ? address
            : throw new RuleViolationException("Enter the full address of a recipe page, such as https://example.com/recipes/soup.");
    }

    /// <summary>Refuses a page the library already has a recipe from, archived ones included, before it is downloaded.</summary>
    public static void PageNotInLibrary(string sourceUrl, IEnumerable<Recipe> library)
    {
        if (library.FirstOrDefault(existing => SamePage(existing.SourceUrl, sourceUrl)) is { } same)
        {
            throw new ConflictException($"That page is already in your recipes{Archived(same)} as {same.Name}, so nothing was added.");
        }
    }

    /// <summary>Refuses a recipe whose name the library already has, archived ones included, as file imports do.</summary>
    public static void NameNotInLibrary(RecipeText recipe, IEnumerable<Recipe> library)
    {
        if (library.FirstOrDefault(existing => existing.Name.Equals(recipe.Name.Trim(), StringComparison.OrdinalIgnoreCase)) is { } same)
        {
            throw new ConflictException($"{same.Name} is already in your recipes{Archived(same)}, so nothing was added. Rename that one to import this.");
        }
    }

    /// <summary>Checks a photo's bytes and says what kind of image it is, judged from the bytes, not the file name.</summary>
    /// <returns>The image's media type, such as image/jpeg.</returns>
    public static string Image(byte[]? data)
    {
        if (data is not { Length: > 0 })
        {
            throw new RuleViolationException("That image is empty. Choose a photo file and try again.");
        }

        if (data.Length > MaxImageBytes)
        {
            throw new RuleViolationException($"That image is larger than {MaxImageBytes / 1024 / 1024} MB. Choose a smaller one.");
        }

        return ImageFormat.Detect(data)
            ?? throw new RuleViolationException("That file isn't a photo ChefBot can show. Choose a JPEG, PNG, WebP or GIF image.");
    }

    /// <summary>Checks the recipe has room for one more photo.</summary>
    public static void RoomForImage(Recipe recipe)
    {
        if (recipe.Images.Count >= MaxImagesPerRecipe)
        {
            throw new RuleViolationException($"A recipe can have up to {MaxImagesPerRecipe} photos. Remove one before adding another.");
        }
    }

    /// <summary>The recipe's photo with that id.</summary>
    public static RecipeImage ImageOn(Recipe recipe, Guid imageId) =>
        recipe.Images.FirstOrDefault(image => image.Id == imageId)
            ?? throw new NotFoundException($"{recipe.Name} has no photo with id {imageId}.");

    // ---------- pantry ----------

    public static void PantryItemName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RuleViolationException("A pantry item needs a name.");
        }
    }

    /// <summary>Something typed in for the shopping list: it needs a name, and an amount above zero if it has one.</summary>
    public static void ShoppingRequest(NewShoppingItem request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RuleViolationException("Type what to buy, for example \"milk\".");
        }

        if (request.Quantity is <= 0m)
        {
            throw new RuleViolationException("An amount must be more than zero. Leave it empty if it doesn't matter.");
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

    // ---------- stores ----------

    /// <summary>
    /// Checks a new list of stores: each has a name of its own, at most one is the main store, and a seasonal store
    /// says which months it is open.
    /// </summary>
    public static IReadOnlyList<GroceryStoreDetails> GroceryStores(IReadOnlyList<GroceryStoreDetails>? stores)
    {
        var list = stores ?? [];
        if (list.Count > MaxStores)
        {
            throw new RuleViolationException($"Keep to {MaxStores} stores or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var store in list)
        {
            var name = store.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every store needs a name.");
            }

            if (name.Length > MaxStoreName)
            {
                throw new RuleViolationException($"Keep store names to {MaxStoreName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two stores called {name}. Give each store its own name.");
            }

            if ((store.Months ?? []).Any(month => month is < 1 or > 12))
            {
                throw new RuleViolationException($"{name} has a month that isn't between 1 and 12.");
            }

            if (store.SeasonalProduce && (store.Months ?? []).Count == 0)
            {
                throw new RuleViolationException($"Choose the months {name} is open, or stop sending in-season produce there.");
            }
        }

        if (list.Count(store => store.IsMain) > 1)
        {
            throw new RuleViolationException("Only one store can be the main store. Choose one and untick the others.");
        }

        return list;
    }

    /// <summary>
    /// Checks the to-do apps to save: each has its own name, an address where its kind needs one, and a token where
    /// its kind needs one (a new one, or one already saved for it in <paramref name="current"/>).
    /// </summary>
    /// <exception cref="RuleViolationException">A target is missing something, or there are too many.</exception>
    public static IReadOnlyList<ShoppingTargetDraft> ShoppingTargets(IReadOnlyList<ShoppingTargetDraft>? targets, IReadOnlyList<ShoppingTarget> current)
    {
        var list = targets ?? [];
        if (list.Count > MaxShoppingTargets)
        {
            throw new RuleViolationException($"Keep to {MaxShoppingTargets} to-do apps or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in list)
        {
            var name = target.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every to-do app needs a name, such as Kitchen list.");
            }

            if (name.Length > MaxStoreName)
            {
                throw new RuleViolationException($"Keep to-do app names to {MaxStoreName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two to-do apps called {name}. Give each its own name.");
            }

            if (!Enum.IsDefined(target.Kind))
            {
                throw new RuleViolationException($"Choose what kind of app {name} is: Home Assistant, Todoist or a webhook.");
            }

            var url = target.Url?.Trim() ?? string.Empty;
            var hasToken = target.Token is { } token
                ? token.Trim().Length > 0
                : current.Any(saved => saved.Id == target.Id && saved.Token.Length > 0);
            switch (target.Kind)
            {
                case ShoppingTargetKind.HomeAssistant when !WebAddress.IsHttp(url):
                    throw new RuleViolationException($"Enter the address {name} is reached at, such as http://homeassistant.local:8123.");
                case ShoppingTargetKind.HomeAssistant when !(target.List?.Trim() ?? string.Empty).StartsWith("todo.", StringComparison.Ordinal):
                    throw new RuleViolationException($"Enter the to-do list for {name} as Home Assistant names it, such as todo.shopping_list.");
                case ShoppingTargetKind.HomeAssistant or ShoppingTargetKind.Todoist when !hasToken:
                    throw new RuleViolationException($"Enter an access token for {name}. {TokenHelp(target.Kind)}");
                case ShoppingTargetKind.Webhook when !WebAddress.IsHttp(url):
                    throw new RuleViolationException($"Enter the webhook address for {name}: a full http or https address.");
            }
        }

        return list;
    }

    /// <summary>One of the saved to-do apps.</summary>
    /// <exception cref="NotFoundException">There is no saved to-do app with that id.</exception>
    public static ShoppingTarget ShoppingTarget(IReadOnlyList<ShoppingTarget> targets, Guid id) =>
        targets.FirstOrDefault(target => target.Id == id)
        ?? throw new NotFoundException("That to-do app isn't set up any more. Reload the page and choose another.");

    private static string TokenHelp(ShoppingTargetKind kind) => kind == ShoppingTargetKind.HomeAssistant
        ? "Make a long-lived access token on your Home Assistant profile page, under Security."
        : "Copy your API token from Todoist's settings, under Integrations, then Developer.";

    /// <summary>The household's store with this name, or null for an empty name, meaning no particular store.</summary>
    public static GroceryStore? StoreNamed(IReadOnlyList<GroceryStore> stores, string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : stores.FirstOrDefault(store => string.Equals(store.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
              ?? throw new NotFoundException($"There is no store called {name.Trim()}. Add it on the Settings tab first.");

    // ---------- plan styles ----------

    /// <summary>
    /// Checks a new list of plan styles: each has a name of its own, plans something, has counts the week can hold,
    /// and has rules that say what they look for. When no style is marked as the default, the first one becomes it.
    /// </summary>
    public static IReadOnlyList<PlanStyleDetails> PlanStyles(IReadOnlyList<PlanStyleDetails>? styles)
    {
        var list = styles ?? [];
        if (list.Count > MaxPlanStyles)
        {
            throw new RuleViolationException($"Keep to {MaxPlanStyles} plan styles or fewer.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var style in list)
        {
            var name = style.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                throw new RuleViolationException("Every plan style needs a name.");
            }

            if (name.Length > MaxPlanStyleName)
            {
                throw new RuleViolationException($"Keep plan style names to {MaxPlanStyleName} characters or fewer.");
            }

            if (!names.Add(name))
            {
                throw new RuleViolationException($"There are two plan styles called {name}. Give each one its own name.");
            }

            PlanStyle(name, style);
        }

        if (list.Count(style => style.IsDefault) > 1)
        {
            throw new RuleViolationException("Only one plan style can be the default. Choose one and untick the others.");
        }

        return list.Count > 0 && !list.Any(style => style.IsDefault)
            ? [list[0] with { IsDefault = true }, .. list.Skip(1)]
            : list;
    }

    private static void PlanStyle(string name, PlanStyleDetails style)
    {
        var days = style.CookingDays ?? [];
        if (days.Any(day => !Enum.IsDefined(day)) || days.Distinct().Count() != days.Count)
        {
            throw new RuleViolationException($"{name} lists a cooking day twice. Choose each day once.");
        }

        if (style.Breakfasts is < 0 or > 5)
        {
            throw new RuleViolationException($"{name} can plan 0 to 5 different breakfasts, one per weekday at most.");
        }

        if (new[] { style.Snacks, style.Sweets, style.Drinks }.Any(count => count is < 0 or > MaxExtras))
        {
            throw new RuleViolationException($"{name} can plan 0 to {MaxExtras} each of snacks, sweets and drinks.");
        }

        if (days.Count + style.Breakfasts + style.Snacks + style.Sweets + style.Drinks == 0)
        {
            throw new RuleViolationException($"{name} doesn't plan anything. Choose some cooking days, or some breakfasts, snacks, sweets or drinks.");
        }

        var rules = style.Rules ?? [];
        if (rules.Count > MaxPlanRules)
        {
            throw new RuleViolationException($"{name} has more than {MaxPlanRules} rules. Remove some.");
        }

        foreach (var rule in rules)
        {
            PlanRule(name, rule);
        }
    }

    private static void PlanRule(string name, PlanRuleDetails rule)
    {
        if (!Enum.IsDefined(rule.Limit) || !Enum.IsDefined(rule.Match) || (rule.Slot is { } slot && !Enum.IsDefined(slot)))
        {
            throw new RuleViolationException($"{name} has a rule ChefBot doesn't understand. Remove it and add it again.");
        }

        if (rule.Slot is null && rule.Limit != RuleLimit.None)
        {
            throw new RuleViolationException($"{name} has a rule for every kind of meal that isn't \"no meals\". Choose a kind of meal, such as dinners, for counted rules.");
        }

        if (rule.Slot == MealSlot.Lunch)
        {
            throw new RuleViolationException($"{name} has a rule for lunches, but lunches are leftovers of dinners. Make it a rule for dinners instead.");
        }

        if (rule.Limit != RuleLimit.None && rule.Count is < 0 or > MaxExtras)
        {
            throw new RuleViolationException($"{name} has a rule counting {rule.Count} meals. Use a number from 0 to {MaxExtras}.");
        }

        if (rule.Match is RecipeMatchKind.Tag or RecipeMatchKind.Ingredient && !(rule.Values ?? []).Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            var what = rule.Match == RecipeMatchKind.Tag ? "tag" : "ingredient";
            throw new RuleViolationException($"{name} has a rule that needs at least one {what}. Type one, or remove the rule.");
        }

        if (rule.Match == RecipeMatchKind.ReadyWithin && rule.Minutes is not (> 0 and <= MaxRuleMinutes))
        {
            throw new RuleViolationException($"{name} has a time rule without a sensible number of minutes. Use a number from 1 to {MaxRuleMinutes}.");
        }

        if (rule.Day is not null && (rule.Slot != MealSlot.Dinner || rule.Limit is RuleLimit.AtMost or RuleLimit.None || !Enum.IsDefined(rule.Day.Value)))
        {
            throw new RuleViolationException($"{name} has a rule with a day that isn't for dinners. Only \"at least\" and \"exactly\" dinner rules can choose a day.");
        }
    }

    /// <summary>The household's style with this id, or null to use the default.</summary>
    public static PlanStyle? StyleWithId(IReadOnlyList<PlanStyle> styles, IReadOnlyList<PlanStyle> starters, Guid? id) =>
        id is not Guid wanted
            ? null
            : styles.Concat(starters).FirstOrDefault(style => style.Id == wanted)
              ?? throw new NotFoundException("That plan style no longer exists. Reload the page and choose another.");

    /// <summary>
    /// Checks a meal can move to <paramref name="date"/>: it is a planned dinner, breakfast or lunch of its own (leftovers
    /// follow their dinner), the day is in the plan's week, and a meal it would swap with isn't cooked or skipped yet.
    /// </summary>
    public static void CanMove(MealPlan plan, PlannedMeal meal, DateOnly date)
    {
        if (meal.Date is null)
        {
            throw new RuleViolationException($"{meal.Title} can be made on any day, so it has no day to move.");
        }

        if (meal.IsLeftover)
        {
            throw new RuleViolationException("Leftover lunches follow their dinner. Move the dinner instead.");
        }

        if (meal.Status != MealStatus.Planned)
        {
            throw new ConflictException($"{meal.Title} is already marked as {meal.Status.ToString().ToLowerInvariant()}. Undo that first to move it.");
        }

        if (date < plan.WeekStart || date > WeekLayout.WeekEnd(plan.WeekStart))
        {
            throw new RuleViolationException("Meals can only move to another day of the same week.");
        }

        var other = plan.Meals.FirstOrDefault(other => other.Id != meal.Id && other.Slot == meal.Slot && other.Date == date && !other.IsLeftover);
        if (other is not null && other.Status != MealStatus.Planned)
        {
            throw new ConflictException($"{other.Title} on that day is already marked as {other.Status.ToString().ToLowerInvariant()}, so the two can't swap.");
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

    private static string Archived(Recipe recipe) => recipe.IsArchived ? " (archived)" : string.Empty;

    /// <summary>Whether two page addresses are the same page, ignoring case and a leading www. in the host, and a trailing slash.</summary>
    private static bool SamePage(string? first, string? second) =>
        Uri.TryCreate(first, UriKind.Absolute, out var a) && Uri.TryCreate(second, UriKind.Absolute, out var b)
        && string.Equals(Normal(a), Normal(b), StringComparison.Ordinal);

    private static string Normal(Uri url) =>
        $"{(url.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? url.Host[4..] : url.Host).ToLowerInvariant()}{url.AbsolutePath.TrimEnd('/')}{url.Query}";
}
