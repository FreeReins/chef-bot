namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>Everything that could replace one meal.</summary>
internal sealed record Suggestions(IReadOnlyList<Alternative> Recipes, IReadOnlyList<MealIdea> Ideas);
