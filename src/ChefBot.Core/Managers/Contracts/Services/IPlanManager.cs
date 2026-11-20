using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>
/// Plans weeks and keeps each plan, its leftover lunches and its shopping list in step. Every change to a plan
/// goes through here: staging, confirming, swapping or cooking meals, and recording purchases.
/// </summary>
public interface IPlanManager
{
    /// <summary>Proposes a plan for a week and saves it as staged. Publishes <see cref="PlanStaged"/>.</summary>
    Task<PlanResponse> StageAsync(StagePlanRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds a week around what is in the pantry, typically after shopping: recipes that use food already
    /// bought count for much more, and the new shopping list only asks for what is still missing.
    /// </summary>
    Task<PlanResponse> RegenerateFromPantryAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Removes a plan. The pantry is left as it is, so anything already bought stays on hand.</summary>
    Task ClearAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>The plan for this week, or else the next upcoming one.</summary>
    Task<PlanResponse> GetCurrentAsync(CancellationToken cancellationToken);

    Task<PlanResponse> GetAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Makes a staged plan the working plan, which opens it up for shopping.</summary>
    Task<PlanResponse> ConfirmAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Recalculates leftovers and the shopping list, for example after the pantry was edited by hand.</summary>
    Task<PlanResponse> RefreshAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Refreshes every plan whose week hasn't ended. Run after <see cref="PantryChanged"/>.</summary>
    Task RefreshOpenPlansAsync(CancellationToken cancellationToken);

    /// <summary>What the plan needs, where each ingredient comes from, and what the pantry holds.</summary>
    Task<IngredientsResponse> GetIngredientsAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Suggests replacements for a meal: recipes that use the pantry, and creative ideas for lunch and dinner.</summary>
    Task<AlternativesResponse> GetAlternativesAsync(Guid planId, Guid mealId, int recipeLimit, int creativeCount, CancellationToken cancellationToken);

    /// <summary>Replaces a meal with a recipe or a free-form meal.</summary>
    Task<PlanResponse> ReplaceMealAsync(Guid planId, Guid mealId, ReplaceMealRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a meal as planned, cooked or skipped. Cooking a meal takes its ingredients out of the pantry where
    /// amounts can be compared. Undoing "cooked" does not put them back.
    /// </summary>
    Task<PlanResponse> SetMealStatusAsync(Guid planId, Guid mealId, MealStatusRequest request, CancellationToken cancellationToken);

    /// <summary>Ticks one shopping item off and puts it in the pantry with an expiration date.</summary>
    Task<PlanResponse> PurchaseAsync(Guid planId, Guid itemId, PurchaseDetails details, CancellationToken cancellationToken);

    /// <summary>Marks everything still on the list as bought on the same day.</summary>
    Task<PlanResponse> PurchaseAllAsync(Guid planId, PurchaseAllRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Stages next week's plan when the weekly schedule's time has come, once per week. When the planner is set to
    /// import from Mealie first, this sends <see cref="ImportMealieRecipes"/> instead and staging follows the import.
    /// </summary>
    Task CheckWeeklyScheduleAsync(CancellationToken cancellationToken);

    /// <summary>Stages a week for the weekly schedule unless it already has a plan.</summary>
    Task StageScheduledWeekAsync(StageScheduledWeek command, CancellationToken cancellationToken);

    /// <summary>When the weekly schedule next stages a plan, or null when it is switched off.</summary>
    DateTimeOffset? NextScheduledStaging();
}
