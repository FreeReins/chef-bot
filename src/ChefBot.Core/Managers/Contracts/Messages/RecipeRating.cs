namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How a recipe is rated: the average of every review it has had.</summary>
/// <param name="Average">The average stars, from 1 to 5, rounded to one decimal place.</param>
/// <param name="Count">How many reviews the average comes from.</param>
public sealed record RecipeRating(decimal Average, int Count);
