namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A stored recipe.</summary>
public sealed record RecipeResponse(
    Guid Id,
    string Name,
    RecipeCourse Course,
    RecipeOrigin Origin,
    bool IsSlowCooker,
    decimal Servings,
    IReadOnlyList<string> Ingredients);
