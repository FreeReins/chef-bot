using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A recipe written the way someone would type it in: ingredients are lines such as "2 cups kale". Sample recipes,
/// recipe files and the recipe form all arrive in this shape.
/// </summary>
/// <param name="Name">The recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="Ingredients">Its ingredient lines.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Servings">How many servings it makes, when known.</param>
/// <param name="Description">A short summary of the dish.</param>
/// <param name="Instructions">The steps, in order.</param>
/// <param name="PrepMinutes">Preparation time in minutes.</param>
/// <param name="CookMinutes">Cooking time in minutes.</param>
/// <param name="TotalMinutes">The whole time in minutes.</param>
/// <param name="SourceUrl">The web page it came from.</param>
/// <param name="Author">Who wrote it.</param>
/// <param name="YieldText">What it makes, in the source's words.</param>
/// <param name="Notes">The cook's notes.</param>
/// <param name="Tags">Its tags and keywords.</param>
/// <param name="ImageUrls">Photos to download, the cover first. The recipe form never sends these.</param>
internal sealed record RecipeText(
    string Name,
    RecipeCourse Course,
    IReadOnlyList<string> Ingredients,
    bool IsSlowCooker = false,
    decimal? Servings = null,
    string? Description = null,
    IReadOnlyList<string>? Instructions = null,
    int? PrepMinutes = null,
    int? CookMinutes = null,
    int? TotalMinutes = null,
    string? SourceUrl = null,
    string? Author = null,
    string? YieldText = null,
    string? Notes = null,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<string>? ImageUrls = null);
