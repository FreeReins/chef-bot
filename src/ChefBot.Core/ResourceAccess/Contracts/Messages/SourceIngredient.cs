namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One ingredient row as an outside source describes it.</summary>
/// <param name="FoodName">The parsed food, when the source has one.</param>
/// <param name="Quantity">The parsed amount, when the source has one.</param>
/// <param name="Unit">The parsed unit, when the source has one.</param>
/// <param name="Text">The free-text line, used when the row was never parsed into a food.</param>
/// <param name="Note">Words kept apart from the food, such as "stems removed".</param>
internal sealed record SourceIngredient(string? FoodName, decimal? Quantity, string? Unit, string? Text, string? Note = null);
