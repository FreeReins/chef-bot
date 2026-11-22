namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe as a row in the library list: enough to recognise it, without its steps or photos.</summary>
/// <param name="Id">The recipe's id.</param>
/// <param name="Name">The recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Ingredients">Its ingredient lines as written.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="CoverUrl">The address of its first stored photo, for a card in the list, or null when it has none.</param>
/// <param name="FavouredBy">The people who have it as a favourite, oldest favourite first.</param>
/// <param name="Rating">The average of its reviews, or null when it has none.</param>
/// <param name="HasSuggestion">True when AI suggestions for the recipe are waiting to be looked at.</param>
public sealed record RecipeSummaryResponse(
    Guid Id,
    string Name,
    RecipeCourse Course,
    bool IsSlowCooker,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Tags,
    string? CoverUrl,
    IReadOnlyList<FavouriteResponse> FavouredBy,
    RecipeRating? Rating,
    bool HasSuggestion = false);
