using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Recipes;
using ChefBot.Core.Shopping;

namespace ChefBot.Core.Planning;

/// <summary>
/// Stages, confirms and reads weekly plans. A staged plan is a proposal: it can be reshuffled
/// and adjusted freely, and becomes the working plan once it is confirmed.
/// </summary>
public sealed class PlanningService(
    IRecipeRepository recipes,
    IMealPlanRepository plans,
    IPantryRepository pantry,
    IUnitOfWork unitOfWork,
    RecipeImportService imports,
    WeekPlanner planner,
    PlanRefresher refresher,
    IngredientOverviewBuilder overview,
    PlannerOptions options,
    TimeProvider clock)
{
    /// <summary>Proposes a plan for a week and saves it as staged.</summary>
    /// <param name="weekStart">The Sunday to plan from. Defaults to the upcoming week.</param>
    /// <param name="replace">Allow replacing a plan that was already confirmed. Staged plans are always replaced.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    public Task<PlanView> StageAsync(DateOnly? weekStart, bool replace, CancellationToken cancellationToken) =>
        StageAsync(weekStart, replace, favorPantry: false, cancellationToken);

    /// <summary>
    /// Rebuilds a week around what is in the pantry, typically after shopping: recipes that use food
    /// already bought count for much more, and the new shopping list only asks for what is still missing.
    /// </summary>
    public async Task<PlanView> RegenerateFromPantryAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        return await StageAsync(plan.WeekStart, replace: true, favorPantry: true, cancellationToken);
    }

    /// <summary>Removes a plan. The pantry is left as it is, so anything already bought stays on hand.</summary>
    public async Task ClearAsync(Guid planId, CancellationToken cancellationToken)
    {
        plans.Remove(await plans.GetRequiredAsync(planId, cancellationToken));
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<PlanView> StageAsync(DateOnly? weekStart, bool replace, bool favorPantry, CancellationToken cancellationToken)
    {
        var start = weekStart ?? WeekLayout.UpcomingWeekStart(clock.Today());
        if (start.DayOfWeek != DayOfWeek.Sunday)
        {
            throw new RuleViolationException("A plan's week must start on a Sunday.");
        }

        var existing = await plans.FindByWeekAsync(start, cancellationToken);
        if (existing is { Status: PlanStatus.Confirmed } && !replace)
        {
            throw new ConflictException($"The week of {start:yyyy-MM-dd} is already confirmed. Pass replace to start it over.");
        }

        var notes = new List<string>();
        await ImportIfConfiguredAsync(notes, cancellationToken);

        var stock = await pantry.SnapshotAsync(clock, options, cancellationToken);
        var draft = planner.Plan(new PlanRequest(
            start,
            await recipes.ListActiveAsync(cancellationToken),
            await plans.RecipeIdsPlannedAsync(start.AddDays(-options.AvoidRepeatDays), start, cancellationToken),
            stock,
            clock.GetUtcNow(),
            // Staging the same week again should offer something different.
            Seed: existing is null ? null : Random.Shared.Next(),
            PantryBoost: favorPantry ? options.Weights.RegeneratePantryBoost : 1));

        if (existing is not null)
        {
            // Saved separately so the old and new plan never compete for the same week.
            plans.Remove(existing);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        plans.Add(draft.Plan);
        var shopping = refresher.Refresh(draft.Plan, stock);
        await unitOfWork.CommitAsync(cancellationToken);

        return new PlanView(draft.Plan, [.. notes, .. draft.Warnings, .. shopping.Notes()]);
    }

    /// <summary>Stages the upcoming week unless a plan for it already exists. Used by the weekly schedule.</summary>
    /// <returns>The new plan, or null when there was nothing to do.</returns>
    public async Task<PlanView?> StageUpcomingIfMissingAsync(CancellationToken cancellationToken)
    {
        var start = WeekLayout.UpcomingWeekStart(clock.Today());
        return await plans.FindByWeekAsync(start, cancellationToken) is null
            ? await StageAsync(start, replace: false, cancellationToken)
            : null;
    }

    public async Task<PlanView> ConfirmAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        plan.Confirm();
        await unitOfWork.CommitAsync(cancellationToken);
        return new PlanView(plan, []);
    }

    /// <summary>The plan for this week, or else the next upcoming one.</summary>
    public async Task<PlanView> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var plan = await plans.FindCurrentAsync(clock.Today(), cancellationToken)
            ?? throw new NotFoundException("There is no plan for this week or a later one yet. Stage one to get started.");
        return new PlanView(plan, []);
    }

    public async Task<PlanView> GetAsync(Guid planId, CancellationToken cancellationToken) =>
        new PlanView(await plans.GetRequiredAsync(planId, cancellationToken), []);

    /// <summary>Recalculates leftovers and the shopping list, for example after the pantry was edited by hand.</summary>
    public async Task<PlanView> RefreshAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        var shopping = refresher.Refresh(plan, await pantry.SnapshotAsync(clock, options, cancellationToken));
        await unitOfWork.CommitAsync(cancellationToken);
        return new PlanView(plan, shopping.Notes().ToList());
    }

    /// <summary>What the plan needs, where each ingredient comes from, and what the pantry holds.</summary>
    public async Task<IngredientOverview> GetIngredientsAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await plans.GetRequiredAsync(planId, cancellationToken);
        return overview.Build(plan, await pantry.ListAsync(cancellationToken), clock.Today());
    }

    private async Task ImportIfConfiguredAsync(List<string> notes, CancellationToken cancellationToken)
    {
        if (!options.ImportFromMealieBeforePlanning || !imports.IsConfigured)
        {
            return;
        }

        try
        {
            await imports.ImportAsync(cancellationToken);
        }
        catch (RecipeSourceException exception)
        {
            notes.Add($"Planned from saved recipes because Mealie couldn't be read: {exception.Message}");
        }
    }
}
