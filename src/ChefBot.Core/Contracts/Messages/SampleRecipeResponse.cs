namespace ChefBot.Core.Contracts.Messages;

/// <summary>A sample recipe offered by the setup wizard.</summary>
/// <param name="Name">The sample recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Servings">How many servings it makes, when known.</param>
/// <param name="Ingredients">Its ingredient lines.</param>
/// <param name="AlreadyAdded">True when a recipe with this name is already in the library.</param>
public sealed record SampleRecipeResponse(
    string Name,
    RecipeCourse Course,
    bool IsSlowCooker,
    decimal? Servings,
    IReadOnlyList<string> Ingredients,
    bool AlreadyAdded);
