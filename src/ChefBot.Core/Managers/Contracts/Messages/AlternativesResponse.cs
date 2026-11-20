namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Ways to replace one meal: recipes ranked by pantry use, and creative ideas.</summary>
public sealed record AlternativesResponse(
    MealResponse Meal,
    IReadOnlyList<RecipeAlternative> Recipes,
    IReadOnlyList<CreativeMeal> Creative);
