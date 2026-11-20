using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>Stored weekly plans, always loaded whole: meals with their recipes, and the shopping list.</summary>
internal interface IPlanAccess
{
    Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken);

    /// <summary>The plan covering <paramref name="today"/>, or else the next upcoming one.</summary>
    Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Plans whose week hasn't ended by <paramref name="today"/>.</summary>
    Task<List<MealPlan>> ListOpenAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Recipes used by plans whose week starts in [<paramref name="from"/>, <paramref name="before"/>).</summary>
    Task<HashSet<Guid>> RecipeIdsPlannedAsync(DateOnly from, DateOnly before, CancellationToken cancellationToken);

    /// <summary>Adds the plan, or updates the stored one with the same id, including its meals and shopping list.</summary>
    Task SaveAsync(MealPlan plan, CancellationToken cancellationToken);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
}
