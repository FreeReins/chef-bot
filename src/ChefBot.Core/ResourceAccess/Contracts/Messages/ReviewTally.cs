namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A recipe's reviews added up: how many there are, and all their stars together.</summary>
/// <param name="Count">How many reviews.</param>
/// <param name="TotalStars">The stars of every review, summed.</param>
internal sealed record ReviewTally(int Count, int TotalStars);
