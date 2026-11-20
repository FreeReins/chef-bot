namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The ingredients view: what the plan needs, and what is in the pantry.</summary>
public sealed record IngredientsResponse(IReadOnlyList<IngredientLine> Ingredients, IReadOnlyList<PantryUseResponse> Pantry);
