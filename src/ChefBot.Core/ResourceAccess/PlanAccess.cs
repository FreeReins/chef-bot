using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

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

    public async Task<PlanTally> TallyAsync(CancellationToken cancellationToken) => new(
        await db.MealPlans.CountAsync(cancellationToken),
        await db.MealPlans.CountAsync(plan => plan.Status == PlanStatus.Confirmed, cancellationToken));

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
