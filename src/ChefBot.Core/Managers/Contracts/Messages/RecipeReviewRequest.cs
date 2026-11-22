namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A new review of a recipe, or new stars and notes for one.</summary>
/// <param name="Stars">From 1 to 5.</param>
/// <param name="Notes">Anything worth remembering about it. Optional.</param>
public sealed record RecipeReviewRequest(int Stars, string? Notes);
