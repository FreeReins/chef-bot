namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe as someone types it in. Ingredients are free-text lines such as "2 cups kale".</summary>
public sealed record RecipeDraft(
    string Name,
    RecipeCourse Course,
    IReadOnlyList<string> Ingredients,
    bool IsSlowCooker = false,
    decimal? Servings = null);
