using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakePlans(FakeRecipes recipes) : IPlanAccess
{
    public List<MealPlan> Rows { get; } = [];

    public Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Load(Rows.FirstOrDefault(p => p.Id == id)));

    public Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken) =>
        Task.FromResult(Load(Rows.FirstOrDefault(p => p.WeekStart == weekStart)));

    public Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken) =>
        Task.FromResult(Load(
            Rows.Where(p => p.WeekStart >= today.AddDays(-6) && p.WeekStart <= today).MaxBy(p => p.WeekStart)
            ?? Rows.Where(p => p.WeekStart > today).MinBy(p => p.WeekStart)));

    public Task<List<MealPlan>> ListOpenAsync(DateOnly today, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(p => p.WeekStart >= today.AddDays(-6)).OrderBy(p => p.WeekStart).Select(p => Load(p)!).ToList());

    public Task<HashSet<Guid>> RecipeIdsPlannedAsync(DateOnly from, DateOnly before, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(p => p.WeekStart >= from && p.WeekStart < before)
            .SelectMany(p => p.Meals).Where(m => m.RecipeId is not null).Select(m => m.RecipeId!.Value).ToHashSet());

    public Task SaveAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        if (Rows.Any(p => p.WeekStart == plan.WeekStart && p.Id != plan.Id))
        {
            throw new InvalidOperationException("Two plans for one week: the database's unique index would refuse this.");
        }

        var stored = Copies.Of(plan);
        stored.Meals.ForEach(meal => meal.Recipe = null); // Only the recipe id is stored.
        Rows.RemoveAll(p => p.Id == plan.Id);
        Rows.Add(stored);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }

    /// <summary>A copy of a stored plan with each meal's recipe loaded, as the database query does.</summary>
    private MealPlan? Load(MealPlan? row)
    {
        if (row is null)
        {
            return null;
        }

        var plan = Copies.Of(row);
        foreach (var meal in plan.Meals.Where(m => m.RecipeId is not null))
        {
            meal.Recipe = recipes.Rows.FirstOrDefault(r => r.Id == meal.RecipeId) is { } recipe ? Copies.Of(recipe) : null;
        }

        return plan;
    }
}
