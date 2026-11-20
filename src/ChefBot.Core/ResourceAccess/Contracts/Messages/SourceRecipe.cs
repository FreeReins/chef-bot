namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A recipe as an outside source describes it.</summary>
/// <param name="ExternalId">Its id in the source.</param>
/// <param name="Name">Its name.</param>
/// <param name="Servings">How many servings it makes, when known.</param>
/// <param name="Labels">Its tags, categories and tools, which decide its course and are kept as tags.</param>
/// <param name="Ingredients">Its ingredient rows.</param>
/// <param name="Description">A short summary of the dish.</param>
/// <param name="Instructions">The steps, in order.</param>
/// <param name="PrepTime">Preparation time as the source writes it, such as "PT15M" or "15 minutes".</param>
/// <param name="CookTime">Cooking time as the source writes it.</param>
/// <param name="TotalTime">The whole time as the source writes it.</param>
/// <param name="SourceUrl">The web page the source got it from.</param>
/// <param name="YieldText">What it makes, in the source's words.</param>
/// <param name="Notes">Notes kept with it.</param>
/// <param name="ImageUrls">Photos to download, the cover first.</param>
internal sealed record SourceRecipe(
    string ExternalId,
    string Name,
    decimal? Servings,
    IReadOnlyList<string> Labels,
    IReadOnlyList<SourceIngredient> Ingredients,
    string? Description = null,
    IReadOnlyList<string>? Instructions = null,
    string? PrepTime = null,
    string? CookTime = null,
    string? TotalTime = null,
    string? SourceUrl = null,
    string? YieldText = null,
    string? Notes = null,
    IReadOnlyList<string>? ImageUrls = null);
