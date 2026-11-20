namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe as someone types it in. Ingredients are free-text lines such as "2 cups kale".</summary>
/// <param name="Name">The recipe's name.</param>
/// <param name="Course">The kind of dish it is.</param>
/// <param name="Ingredients">One line per ingredient, as written.</param>
/// <param name="IsSlowCooker">True when it is made in a slow cooker.</param>
/// <param name="Servings">How many servings it makes. Null keeps the current number, or the default for a new recipe.</param>
/// <param name="Description">A short summary of the dish.</param>
/// <param name="Instructions">The steps, in order.</param>
/// <param name="PrepMinutes">Preparation time in minutes.</param>
/// <param name="CookMinutes">Cooking time in minutes.</param>
/// <param name="TotalMinutes">The whole time from start to table, in minutes.</param>
/// <param name="SourceUrl">The web page the recipe came from: a full http or https address.</param>
/// <param name="Author">Who wrote the recipe.</param>
/// <param name="YieldText">What it makes, in words: "one 9-inch pie".</param>
/// <param name="Notes">The cook's own notes.</param>
/// <param name="Tags">Tags such as "vegetarian" or "quick".</param>
public sealed record RecipeDraft(
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
    IReadOnlyList<string>? Tags = null);
