namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>One ingredient row as an outside source describes it.</summary>
/// <param name="FoodName">The parsed food, when the source has one.</param>
/// <param name="Quantity">The parsed amount, when the source has one.</param>
/// <param name="Unit">The parsed unit, when the source has one.</param>
/// <param name="Text">The free-text line, used when the row was never parsed into a food.</param>
internal sealed record SourceIngredient(string? FoodName, decimal? Quantity, string? Unit, string? Text);
