namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A stored recipe.</summary>
/// <param name="Id">The recipe's id.</param>
/// <param name="Name">The recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Servings">How many servings it makes.</param>
/// <param name="Ingredients">Its ingredient lines as written, ready to show or edit.</param>
/// <param name="Description">A short summary of the dish.</param>
/// <param name="Instructions">The steps, in order.</param>
/// <param name="PrepMinutes">Preparation time in minutes.</param>
/// <param name="CookMinutes">Cooking time in minutes.</param>
/// <param name="TotalMinutes">The whole time from start to table, in minutes.</param>
/// <param name="SourceUrl">The web page it came from.</param>
/// <param name="Author">Who wrote it.</param>
/// <param name="YieldText">What it makes, in words.</param>
/// <param name="Notes">The cook's own notes.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="Images">Its stored photos, the cover first. Photos still downloading aren't listed.</param>
/// <param name="IngredientDetails">The same ingredients as <paramref name="Ingredients"/>, as ChefBot understood them for shopping.</param>
/// <param name="StepDetails">The same steps as <paramref name="Instructions"/>, as ChefBot read them for cooking mode.</param>
/// <param name="FavouredBy">The people who have it as a favourite, oldest favourite first.</param>
/// <param name="Rating">The average of its reviews, or null when it has none.</param>
/// <param name="HasSuggestion">True when AI suggestions for the recipe are waiting to be looked at.</param>
public sealed record RecipeResponse(
    Guid Id,
    string Name,
    RecipeCourse Course,
    bool IsSlowCooker,
    decimal Servings,
    IReadOnlyList<string> Ingredients,
    string? Description,
    IReadOnlyList<string> Instructions,
    int? PrepMinutes,
    int? CookMinutes,
    int? TotalMinutes,
    string? SourceUrl,
    string? Author,
    string? YieldText,
    string? Notes,
    IReadOnlyList<string> Tags,
    IReadOnlyList<RecipeImageResponse> Images,
    IReadOnlyList<RecipeIngredientResponse> IngredientDetails,
    IReadOnlyList<RecipeStepResponse> StepDetails,
    IReadOnlyList<FavouriteResponse> FavouredBy,
    RecipeRating? Rating,
    bool HasSuggestion = false);
