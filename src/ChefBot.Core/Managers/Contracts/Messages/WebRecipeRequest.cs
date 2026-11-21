namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe page to import, by its address, such as https://example.com/recipes/soup.</summary>
public sealed record WebRecipeRequest(string Url);
