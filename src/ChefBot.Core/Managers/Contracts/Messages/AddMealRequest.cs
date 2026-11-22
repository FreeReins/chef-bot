namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Adds a recipe to a week's plan, from the recipe's page. A week with no plan yet gets a staged one holding just
/// this meal. The shopping list follows shortly after, once <see cref="MealAdded"/> is handled.
/// </summary>
/// <param name="RecipeId">The recipe to cook.</param>
/// <param name="WeekStart">The Sunday of the week to add it to.</param>
/// <param name="Date">The day, inside that week. Null for a snack, sweet or drink made whenever suits.</param>
/// <param name="Slot">Which meal it is.</param>
/// <param name="Servings">How many to cook for. Null sizes it for the plan's number of people, like a planned meal.</param>
/// <param name="Replaces">A meal on the plan to swap for this recipe. Null adds it alongside whatever is there.</param>
public sealed record AddMealRequest(
    Guid RecipeId,
    DateOnly WeekStart,
    DateOnly? Date,
    MealSlot Slot,
    int? Servings = null,
    Guid? Replaces = null);
