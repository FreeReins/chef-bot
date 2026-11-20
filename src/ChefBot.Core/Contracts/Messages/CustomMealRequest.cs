namespace ChefBot.Core.Contracts.Messages;

/// <summary>A free-form meal, for example one of the creative suggestions.</summary>
public sealed record CustomMealRequest(string Title, IReadOnlyList<string> Ingredients);
