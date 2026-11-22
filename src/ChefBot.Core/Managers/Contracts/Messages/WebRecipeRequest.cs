namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe page to import, by its address, such as https://example.com/recipes/soup.</summary>
/// <param name="Url">The page's address.</param>
/// <param name="CookbookId">A cookbook to put the recipe in; null for none.</param>
public sealed record WebRecipeRequest(string Url, Guid? CookbookId = null);
