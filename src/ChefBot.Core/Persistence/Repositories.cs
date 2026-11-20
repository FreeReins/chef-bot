using ChefBot.Core.Abstractions;
using ChefBot.Core.Accounts;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.Persistence;

/// <inheritdoc cref="IRecipeRepository"/>
public sealed class RecipeRepository(ChefBotDbContext db) : IRecipeRepository
{
    private IQueryable<Recipe> WithIngredients => db.Recipes.Include(recipe => recipe.Ingredients);

    public Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        WithIngredients.Where(recipe => !recipe.IsArchived).OrderBy(recipe => recipe.Name).ToListAsync(cancellationToken);

    public Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) =>
        WithIngredients.ToListAsync(cancellationToken);

    public Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        WithIngredients.FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);

    public void Add(Recipe recipe) => db.Recipes.Add(recipe);
}

/// <inheritdoc cref="IMealPlanRepository"/>
public sealed class MealPlanRepository(ChefBotDbContext db) : IMealPlanRepository
{
    /// <summary>Plans are always loaded whole: meals, their recipes and ingredients, and the shopping list.</summary>
    private IQueryable<MealPlan> Complete => db.MealPlans
        .Include(plan => plan.Meals).ThenInclude(meal => meal.Recipe!).ThenInclude(recipe => recipe.Ingredients)
        .Include(plan => plan.Meals).ThenInclude(meal => meal.CustomIngredients)
        .Include(plan => plan.ShoppingItems)
        .AsSplitQuery();

    public Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Complete.FirstOrDefaultAsync(plan => plan.Id == id, cancellationToken);

    public Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken) =>
        Complete.FirstOrDefaultAsync(plan => plan.WeekStart == weekStart, cancellationToken);

    public async Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var earliestStart = today.AddDays(-6);
        return await Complete
                .Where(plan => plan.WeekStart >= earliestStart && plan.WeekStart <= today)
                .OrderByDescending(plan => plan.WeekStart)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await Complete
                .Where(plan => plan.WeekStart > today)
                .OrderBy(plan => plan.WeekStart)
                .FirstOrDefaultAsync(cancellationToken);
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

    public void Add(MealPlan plan) => db.MealPlans.Add(plan);

    public void Remove(MealPlan plan) => db.MealPlans.Remove(plan);
}

/// <inheritdoc cref="IPantryRepository"/>
public sealed class PantryRepository(ChefBotDbContext db) : IPantryRepository
{
    public Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken) =>
        db.PantryItems.ToListAsync(cancellationToken);

    public Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.PantryItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void Add(PantryItem item) => db.PantryItems.Add(item);

    public void Remove(PantryItem item) => db.PantryItems.Remove(item);
}

/// <inheritdoc cref="IUserRepository"/>
public sealed class UserRepository(ChefBotDbContext db) : IUserRepository
{
    public Task<bool> AnyAsync(CancellationToken cancellationToken) => db.Users.AnyAsync(cancellationToken);

    public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

    public Task<List<User>> ListAsync(CancellationToken cancellationToken) => db.Users.ToListAsync(cancellationToken);

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}

/// <inheritdoc cref="IAccessLinkRepository"/>
public sealed class AccessLinkRepository(ChefBotDbContext db) : IAccessLinkRepository
{
    public Task<AccessLink?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.AccessLinks.FirstOrDefaultAsync(link => link.Id == id, cancellationToken);

    public Task<AccessLink?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.AccessLinks.FirstOrDefaultAsync(link => link.TokenHash == tokenHash, cancellationToken);

    public Task<List<AccessLink>> ListAsync(CancellationToken cancellationToken) => db.AccessLinks.ToListAsync(cancellationToken);

    public void Add(AccessLink link) => db.AccessLinks.Add(link);

    public void Remove(AccessLink link) => db.AccessLinks.Remove(link);
}

/// <inheritdoc cref="ISettingsStore"/>
public sealed class SettingsStore(ChefBotDbContext db) : ISettingsStore
{
    public async Task<TSettings> GetAsync<TSettings>(CancellationToken cancellationToken)
        where TSettings : class, new()
    {
        // Local first, so defaults added earlier in the same request are found before they are saved.
        var set = db.Set<TSettings>();
        var current = set.Local.FirstOrDefault() ?? await set.FirstOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            current = new TSettings();
            set.Add(current);
        }

        return current;
    }
}

/// <inheritdoc cref="IUnitOfWork"/>
public sealed class EfUnitOfWork(ChefBotDbContext db) : IUnitOfWork
{
    public Task CommitAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
