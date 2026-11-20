namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One ingredient line of a recipe, with what ChefBot read from it for the shopping list.</summary>
/// <param name="Text">The line as written: "2 cups kale, stems removed".</param>
/// <param name="Quantity">The amount, when the line gives one.</param>
/// <param name="Unit">The unit of <paramref name="Quantity"/>, or empty.</param>
/// <param name="Name">The food, as shown on the shopping list.</param>
/// <param name="Note">Words the source keeps apart from the food, such as "stems removed".</param>
public sealed record RecipeIngredientResponse(string Text, decimal? Quantity, string Unit, string Name, string? Note);
