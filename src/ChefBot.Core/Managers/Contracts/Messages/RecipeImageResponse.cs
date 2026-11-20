namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One stored photo of a recipe.</summary>
/// <param name="Id">The photo's id. A photo's bytes never change, so the address can be cached for good.</param>
/// <param name="Url">Where the web page loads it from: <c>/api/recipes/{id}/images/{imageId}</c>.</param>
public sealed record RecipeImageResponse(Guid Id, string Url);
