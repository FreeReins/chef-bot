using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Adding a recipe to a plan from its page. The leftovers and shopping list catch up through <see cref="MealAdded"/>.</content>
internal sealed partial class PlanManager
{
    public async Task<PlanResponse> AddMealAsync(AddMealRequest request, CancellationToken cancellationToken)
    {
        ValidationEngine.AddMeal(request);
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        var recipe = await validation.RecipeAsync(request.RecipeId, cancellationToken);
        ValidationEngine.CanBePlanned(recipe);

        var plan = await plans.FindByWeekAsync(request.WeekStart, cancellationToken);
        var notes = new List<string>();
        if (plan is null)
        {
            var people = (await settings.GetPlannerAsync(cancellationToken)).People;
            plan = PlanningEngine.EmptyPlan(request.WeekStart, await StyleAsync(null, cancellationToken), people, clock.GetUtcNow());
            notes.Add($"Started a plan for the week of {request.WeekStart:MMMM d}.");
        }

        PlannedMeal meal;
        if (request.Replaces is Guid replaces)
        {
            meal = ValidationEngine.MealToReplace(plan, replaces, request);
            PlanningEngine.UseRecipe(meal, recipe, plan.People);
            PlanningEngine.ChoosePortions(meal, request.Servings);
        }
        else
        {
            meal = PlanningEngine.AddRecipe(plan, recipe, request.Date, request.Slot, request.Servings);
        }

        await plans.SaveAsync(plan, cancellationToken);
        await messages.PublishAsync(new MealAdded(plan.Id, meal.Id));
        await messages.CommitAsync(cancellationToken);
        return plan.ToResponse(notes);
    }

    public async Task<bool> CatchUpAsync(Guid planId, CancellationToken cancellationToken)
    {
        using var _ = await writeLock.AcquireAsync(cancellationToken);
        if (await plans.FindAsync(planId, cancellationToken) is not { } plan)
        {
            return false;
        }

        await ReconcileAsync(plan, await SnapshotAsync(cancellationToken), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return true;
    }
}
