namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A meal idea built from a whole grain, a protein and a vegetable or fruit.</summary>
internal sealed record MealIdea(string Title, IReadOnlyList<IdeaPart> Parts);
