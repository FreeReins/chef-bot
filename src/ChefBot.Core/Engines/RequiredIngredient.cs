using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines;

/// <summary>An ingredient a plan needs, scaled to the portions being cooked.</summary>
/// <param name="Ingredient">What is needed.</param>
/// <param name="Quantity">How much, or null when the recipe doesn't say.</param>
/// <param name="Unit">The unit of <paramref name="Quantity"/>.</param>
/// <param name="IsOptional">True when only optional meals need it.</param>
/// <param name="UsedIn">The meal that needs it.</param>
internal sealed record RequiredIngredient(IIngredientRef Ingredient, decimal? Quantity, string Unit, bool IsOptional, string UsedIn);
