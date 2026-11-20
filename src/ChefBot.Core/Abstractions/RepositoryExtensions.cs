using ChefBot.Core.Common;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Abstractions;

/// <summary>Lookups shared by the application services.</summary>
public static class RepositoryExtensions
{
    public static async Task<MealPlan> GetRequiredAsync(this IMealPlanRepository plans, Guid id, CancellationToken cancellationToken)
    {
        return await plans.FindAsync(id, cancellationToken)
            ?? throw new NotFoundException($"There is no meal plan with id {id}.");
    }

    public static async Task<Recipe> GetRequiredAsync(this IRecipeRepository recipes, Guid id, CancellationToken cancellationToken)
    {
        return await recipes.FindAsync(id, cancellationToken)
            ?? throw new NotFoundException($"There is no recipe with id {id}.");
    }

    public static async Task<PantryItem> GetRequiredAsync(this IPantryRepository pantry, Guid id, CancellationToken cancellationToken)
    {
        return await pantry.FindAsync(id, cancellationToken)
            ?? throw new NotFoundException($"There is no pantry item with id {id}.");
    }

    /// <summary>The pantry as of today, with expired items left out.</summary>
    public static async Task<PantrySnapshot> SnapshotAsync(
        this IPantryRepository pantry, TimeProvider clock, PlannerOptions options, CancellationToken cancellationToken)
    {
        var items = await pantry.ListAsync(cancellationToken);
        return new PantrySnapshot(items, clock.Today(), options.ExpiringSoonDays);
    }
}
