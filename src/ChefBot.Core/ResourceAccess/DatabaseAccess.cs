using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

// The accessors backed by PostgreSQL. Reads go through the change tracker, so a row saved earlier in the same
// request is seen as saved even before the commit.

/// <inheritdoc cref="IRecipeAccess"/>
internal sealed class RecipeAccess(ChefBotDbContext db) : IRecipeAccess
{
    private IQueryable<RecipeEntity> WithIngredients => db.Recipes.Include(recipe => recipe.Ingredients);

    public async Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        (await WithIngredients.Where(recipe => !recipe.IsArchived).OrderBy(recipe => recipe.Name).ToListAsync(cancellationToken))
            .Select(recipe => recipe.ToContract()).ToList();

    public async Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) =>
        (await WithIngredients.ToListAsync(cancellationToken)).Select(recipe => recipe.ToContract()).ToList();

    public async Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(id, cancellationToken))?.ToContract();

    public async Task SaveAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        var fresh = recipe.ToEntity();
        var tracked = await TrackedAsync(recipe.Id, cancellationToken);
        if (tracked is null)
        {
            db.Recipes.Add(fresh);
            return;
        }

        db.CopyInto(tracked, fresh);
        db.SyncChildren(tracked.Ingredients, fresh.Ingredients, ingredient => ingredient.Id);
    }

    private async Task<RecipeEntity?> TrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Recipes.Local.FirstOrDefault(recipe => recipe.Id == id)
        ?? await WithIngredients.FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);
}

/// <inheritdoc cref="IPlanAccess"/>
internal sealed class PlanAccess(ChefBotDbContext db) : IPlanAccess
{
    private IQueryable<MealPlanEntity> Complete => db.MealPlans
        .Include(plan => plan.Meals).ThenInclude(meal => meal.Recipe!).ThenInclude(recipe => recipe.Ingredients)
        .Include(plan => plan.Meals).ThenInclude(meal => meal.CustomIngredients)
        .Include(plan => plan.ShoppingItems)
        .AsSplitQuery();

    public async Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await TrackedAsync(id, cancellationToken))?.ToContract();

    public async Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken) =>
        (await Complete.FirstOrDefaultAsync(plan => plan.WeekStart == weekStart, cancellationToken))?.ToContract();

    public async Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var earliestStart = today.AddDays(-6);
        var plan = await Complete
                .Where(plan => plan.WeekStart >= earliestStart && plan.WeekStart <= today)
                .OrderByDescending(plan => plan.WeekStart)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await Complete
                .Where(plan => plan.WeekStart > today)
                .OrderBy(plan => plan.WeekStart)
                .FirstOrDefaultAsync(cancellationToken);
        return plan?.ToContract();
    }

    public async Task<List<MealPlan>> ListOpenAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var earliestStart = today.AddDays(-6);
        var plans = await Complete.Where(plan => plan.WeekStart >= earliestStart).OrderBy(plan => plan.WeekStart).ToListAsync(cancellationToken);
        return plans.Select(plan => plan.ToContract()).ToList();
    }

    public async Task<HashSet<Guid>> RecipeIdsPlannedAsync(DateOnly from, DateOnly before, CancellationToken cancellationToken)
    {
        var ids = await db.MealPlans
            .Where(plan => plan.WeekStart >= from && plan.WeekStart < before)
            .SelectMany(plan => plan.Meals)
            .Where(meal => meal.RecipeId != null)
            .Select(meal => meal.RecipeId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task SaveAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        var fresh = plan.ToEntity();
        var tracked = await TrackedAsync(plan.Id, cancellationToken);
        if (tracked is null)
        {
            db.MealPlans.Add(fresh);
            return;
        }

        db.CopyInto(tracked, fresh);
        db.SyncChildren(tracked.Meals, fresh.Meals, meal => meal.Id, (current, wanted) =>
            db.SyncChildren(current.CustomIngredients, wanted.CustomIngredients, ingredient => ingredient.Id));
        db.SyncChildren(tracked.ShoppingItems, fresh.ShoppingItems, item => item.Id);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await TrackedAsync(id, cancellationToken) is { } plan)
        {
            db.MealPlans.Remove(plan);
        }
    }

    private Task<MealPlanEntity?> TrackedAsync(Guid id, CancellationToken cancellationToken) =>
        Complete.FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);
}

/// <inheritdoc cref="IPantryAccess"/>
internal sealed class PantryAccess(ChefBotDbContext db) : IPantryAccess
{
    public async Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken) =>
        (await db.PantryItems.ToListAsync(cancellationToken)).Select(item => item.ToContract()).ToList();

    public async Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.PantryItems.FindAsync([id], cancellationToken))?.ToContract();

    public async Task SaveAsync(PantryItem item, CancellationToken cancellationToken)
    {
        var fresh = item.ToEntity();
        if (await db.PantryItems.FindAsync([item.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.PantryItems.Add(fresh);
        }
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.PantryItems.FindAsync([id], cancellationToken) is { } item)
        {
            db.PantryItems.Remove(item);
        }
    }
}

/// <inheritdoc cref="IAccountAccess"/>
internal sealed class AccountAccess(ChefBotDbContext db) : IAccountAccess
{
    public async Task<bool> AnyUsersAsync(CancellationToken cancellationToken) =>
        db.Users.Local.Count > 0 || await db.Users.AnyAsync(cancellationToken);

    public async Task<UserAccount?> FindUserAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.Users.FindAsync([id], cancellationToken))?.ToContract();

    public async Task<UserAccount?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken) =>
        (db.Users.Local.FirstOrDefault(user => user.Username == username)
            ?? await db.Users.FirstOrDefaultAsync(user => user.Username == username, cancellationToken))?.ToContract();

    public async Task<List<UserAccount>> ListUsersAsync(CancellationToken cancellationToken) =>
        (await db.Users.ToListAsync(cancellationToken)).Select(user => user.ToContract()).ToList();

    public async Task SaveUserAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var fresh = user.ToEntity();
        if (await db.Users.FindAsync([user.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Users.Add(fresh);
        }
    }

    public async Task RemoveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.Users.FindAsync([id], cancellationToken) is { } user)
        {
            db.Users.Remove(user);
        }
    }

    public async Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken) =>
        (await db.AccessLinks.FindAsync([id], cancellationToken))?.ToContract();

    public async Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        (await db.AccessLinks.FirstOrDefaultAsync(link => link.TokenHash == tokenHash, cancellationToken))?.ToContract();

    public async Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken) =>
        (await db.AccessLinks.ToListAsync(cancellationToken)).Select(link => link.ToContract()).ToList();

    public async Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken)
    {
        var fresh = link.ToEntity();
        if (await db.AccessLinks.FindAsync([link.Id], cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.AccessLinks.Add(fresh);
        }
    }

    public async Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await db.AccessLinks.FindAsync([id], cancellationToken) is { } link)
        {
            db.AccessLinks.Remove(link);
        }
    }
}

/// <inheritdoc cref="ISettingsAccess"/>
internal sealed class SettingsAccess(ChefBotDbContext db) : ISettingsAccess
{
    public async Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) =>
        await RowAsync<SecuritySettingsEntity>(cancellationToken) is { } row
            ? new SecuritySettings { MaxFailedSignIns = row.MaxFailedSignIns, LockoutMinutes = row.LockoutMinutes, RequireHttps = row.RequireHttps }
            : new SecuritySettings();

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new SecuritySettingsEntity
        {
            MaxFailedSignIns = settings.MaxFailedSignIns,
            LockoutMinutes = settings.LockoutMinutes,
            RequireHttps = settings.RequireHttps,
        }, cancellationToken);

    public async Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) =>
        await RowAsync<NotificationSettingsEntity>(cancellationToken) is { } row
            ? new NotificationSettings
            {
                WebhookUrl = row.WebhookUrl,
                PlanReady = row.PlanReady,
                SlowCooker = row.SlowCooker,
                MealsToday = row.MealsToday,
                ExpiringFood = row.ExpiringFood,
                Shopping = row.Shopping,
                DailyTime = row.DailyTime,
                LastDailyRunOn = row.LastDailyRunOn,
            }
            : new NotificationSettings();

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new NotificationSettingsEntity
        {
            WebhookUrl = settings.WebhookUrl,
            PlanReady = settings.PlanReady,
            SlowCooker = settings.SlowCooker,
            MealsToday = settings.MealsToday,
            ExpiringFood = settings.ExpiringFood,
            Shopping = settings.Shopping,
            DailyTime = settings.DailyTime,
            LastDailyRunOn = settings.LastDailyRunOn,
        }, cancellationToken);

    public async Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) =>
        await RowAsync<BackupSettingsEntity>(cancellationToken) is { } row
            ? new BackupSettings
            {
                Enabled = row.Enabled,
                Time = row.Time,
                KeepCount = row.KeepCount,
                KeepDays = row.KeepDays,
                LastRunOn = row.LastRunOn,
                LastResult = row.LastResult,
            }
            : new BackupSettings();

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new BackupSettingsEntity
        {
            Enabled = settings.Enabled,
            Time = settings.Time,
            KeepCount = settings.KeepCount,
            KeepDays = settings.KeepDays,
            LastRunOn = settings.LastRunOn,
            LastResult = settings.LastResult,
        }, cancellationToken);

    public async Task<MealieSettings> GetMealieAsync(CancellationToken cancellationToken) =>
        await RowAsync<MealieSettingsEntity>(cancellationToken) is { } row
            ? new MealieSettings { Url = row.Url, ApiToken = row.ApiToken }
            : new MealieSettings();

    public Task SaveMealieAsync(MealieSettings settings, CancellationToken cancellationToken) =>
        SaveRowAsync(new MealieSettingsEntity { Url = settings.Url, ApiToken = settings.ApiToken }, cancellationToken);

    public async Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken) =>
        await RowAsync<ScheduleStateEntity>(cancellationToken) is { } row
            ? new ScheduleState { LastStagedWeek = row.LastStagedWeek }
            : new ScheduleState();

    public Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken) =>
        SaveRowAsync(new ScheduleStateEntity { LastStagedWeek = state.LastStagedWeek }, cancellationToken);

    /// <summary>The single row of a settings table, if one has been saved (or added earlier in this request).</summary>
    private async Task<TRow?> RowAsync<TRow>(CancellationToken cancellationToken)
        where TRow : class
    {
        var set = db.Set<TRow>();
        return set.Local.FirstOrDefault() ?? await set.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SaveRowAsync<TRow>(TRow fresh, CancellationToken cancellationToken)
        where TRow : class
    {
        if (await RowAsync<TRow>(cancellationToken) is { } tracked)
        {
            db.CopyInto(tracked, fresh);
        }
        else
        {
            db.Set<TRow>().Add(fresh);
        }
    }
}
