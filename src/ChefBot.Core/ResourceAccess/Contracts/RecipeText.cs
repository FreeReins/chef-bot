using ChefBot.Core.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>
/// A recipe written the way someone would type it in: ingredients are lines such as "2 cups kale". Sample recipes,
/// recipe files and the recipe form all arrive in this shape.
/// </summary>
internal sealed record RecipeText(
    string Name,
    RecipeCourse Course,
    IReadOnlyList<string> Ingredients,
    bool IsSlowCooker = false,
    decimal? Servings = null);
