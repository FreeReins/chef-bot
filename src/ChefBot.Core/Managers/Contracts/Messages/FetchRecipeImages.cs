namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command: download the photos an import found for one recipe, so ChefBot keeps its own copies. Sent after the
/// import is saved; handled by calling <see cref="Services.IRecipeManager.FetchImagesAsync"/>.
/// </summary>
/// <param name="RecipeId">The recipe whose photos to download.</param>
public sealed record FetchRecipeImages(Guid RecipeId);
