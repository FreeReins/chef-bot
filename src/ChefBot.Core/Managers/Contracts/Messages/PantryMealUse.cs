namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A planned meal that is going to use a pantry item.</summary>
/// <param name="MealId">The planned meal's id.</param>
/// <param name="Title">The meal's name.</param>
/// <param name="Date">The day it is planned for. Null for snacks and sweets, which have no set day.</param>
/// <param name="Slot">Which part of the day it fills.</param>
public sealed record PantryMealUse(Guid MealId, string Title, DateOnly? Date, MealSlot Slot);
