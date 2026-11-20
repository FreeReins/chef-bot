namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>Everything the plan needs next to everything on hand.</summary>
internal sealed record IngredientOverview(IReadOnlyList<OverviewLine> Ingredients, IReadOnlyList<PantryLine> Pantry);
