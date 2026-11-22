namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One entry on a plan.</summary>
/// <param name="Id">The meal's id.</param>
/// <param name="Date">The suggested day. Null for snacks and sweets.</param>
/// <param name="Day">The weekday name of <paramref name="Date"/>.</param>
/// <param name="Slot">Which part of the day or week the meal fills.</param>
/// <param name="Title">The name shown for the meal.</param>
/// <param name="RecipeId">The recipe it follows. Null for a free-form meal.</param>
/// <param name="Status">Whether the meal is planned, cooked or skipped.</param>
/// <param name="IsOptional">True when the meal is planned but its groceries are marked as skippable.</param>
/// <param name="IsLeftover">True when the meal is eating up leftovers of another meal.</param>
/// <param name="IsSlowCooker">True when the meal is made in a slow cooker.</param>
/// <param name="Portions">Portions to cook. Zero means "make the recipe once, as written".</param>
/// <param name="Ingredients">The names of the ingredients the meal uses.</param>
/// <param name="PortionsChosen">True when someone chose how many the meal is for, so it doesn't follow the plan's number of people.</param>
public sealed record MealResponse(
    Guid Id,
    DateOnly? Date,
    string? Day,
    MealSlot Slot,
    string Title,
    Guid? RecipeId,
    MealStatus Status,
    bool IsOptional,
    bool IsLeftover,
    bool IsSlowCooker,
    int Portions,
    IReadOnlyList<string> Ingredients,
    bool PortionsChosen = false);
