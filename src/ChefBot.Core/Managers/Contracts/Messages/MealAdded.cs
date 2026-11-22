using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Event: a recipe was added to a plan, or swapped in for one of its meals, from the recipe's page. Its handler brings
/// the plan's leftover lunches and shopping list in line with the pantry.
/// </summary>
/// <param name="PlanId">The plan.</param>
/// <param name="MealId">The meal that was added or swapped.</param>
[MessageIdentity("chefbot.meal-added.v1")]
public sealed record MealAdded(Guid PlanId, Guid MealId);
