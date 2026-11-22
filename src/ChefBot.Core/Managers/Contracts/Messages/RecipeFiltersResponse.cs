namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What the recipe library can be filtered by.</summary>
/// <param name="Tags">Every tag on a recipe that can be planned, once each in any case: the most used first, then alphabetically.</param>
/// <param name="People">The people who have at least one favourite, in the order they first chose one.</param>
public sealed record RecipeFiltersResponse(IReadOnlyList<RecipeTagResponse> Tags, IReadOnlyList<FavouriteResponse> People);
