namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What the recipe library can be filtered by.</summary>
/// <param name="Tags">Every tag on a recipe that can be planned, once each in any case: the most used first, then alphabetically.</param>
/// <param name="People">The people who have at least one favourite, in the order they first chose one.</param>
/// <param name="Cookbooks">Every cookbook, by name. Empty until someone makes one, so pages can leave cookbooks out until then.</param>
public sealed record RecipeFiltersResponse(
    IReadOnlyList<RecipeTagResponse> Tags, IReadOnlyList<FavouriteResponse> People, IReadOnlyList<CookbookResponse> Cookbooks);
