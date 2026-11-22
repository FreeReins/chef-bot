using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>Everything the planner needs to propose a week.</summary>
/// <param name="WeekStart">The Sunday the week starts on.</param>
/// <param name="Recipes">The recipes that may be planned.</param>
/// <param name="RecentRecipeIds">Recipes planned recently, avoided where possible.</param>
/// <param name="Pantry">What is on hand, so expiring food gets used.</param>
/// <param name="CreatedAt">The timestamp to record on the plan.</param>
/// <param name="Seed">Fixes the random choices. Null gives the same plan for the same week every time.</param>
/// <param name="PantryBoost">Above 1 to lean harder on food already in the pantry, as when rebuilding after shopping.</param>
/// <param name="Style">How the week is laid out and the rules for its recipes. Defaults to <see cref="PlanStyle.Classic"/>.</param>
/// <param name="Kept">Meals to carry into the new plan unchanged, such as the ones kept when shuffling a week.</param>
/// <param name="Fans">How many people have each recipe as a favourite. Recipes nobody favours are left out.</param>
internal sealed record PlanRequest(
    DateOnly WeekStart,
    IReadOnlyList<Recipe> Recipes,
    IReadOnlySet<Guid> RecentRecipeIds,
    PantrySnapshot Pantry,
    DateTimeOffset CreatedAt,
    int? Seed = null,
    double PantryBoost = 1,
    PlanStyle? Style = null,
    IReadOnlyList<PlannedMeal>? Kept = null,
    IReadOnlyDictionary<Guid, int>? Fans = null);
