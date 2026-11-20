using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Abstractions;

/// <summary>Stored recipes. Returned entities are tracked: changes are saved by <see cref="IUnitOfWork"/>.</summary>
public interface IRecipeRepository
{
    /// <summary>Recipes that can be planned, with their ingredients.</summary>
    Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Every recipe, archived ones included.</summary>
    Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken);

    Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(Recipe recipe);
}

/// <summary>Stored weekly plans with their meals and shopping items.</summary>
public interface IMealPlanRepository
{
    Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken);

    /// <summary>The plan covering <paramref name="today"/>, or else the next upcoming one.</summary>
    Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Recipes used by plans whose week starts in [<paramref name="from"/>, <paramref name="before"/>).</summary>
    Task<HashSet<Guid>> RecipeIdsPlannedAsync(DateOnly from, DateOnly before, CancellationToken cancellationToken);

    void Add(MealPlan plan);

    void Remove(MealPlan plan);
}

/// <summary>What is in the kitchen right now.</summary>
public interface IPantryRepository
{
    Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken);

    Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(PantryItem item);

    void Remove(PantryItem item);
}

/// <summary>Saves every change made through the repositories as one transaction.</summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}
