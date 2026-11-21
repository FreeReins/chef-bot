using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>The recipe library in ChefBot's own database, and importing into it from files, samples and Mealie.</summary>
public interface IRecipeManager
{
    /// <summary>Recipes that can be planned, by name.</summary>
    Task<IReadOnlyList<RecipeResponse>> ListAsync(CancellationToken cancellationToken);

    Task<RecipeResponse> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken);

    /// <summary>Saves an edit. An imported recipe becomes a local one, so later imports leave the edit alone.</summary>
    Task<RecipeResponse> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken);

    /// <summary>Stops a recipe from being planned. Past plans keep showing it.</summary>
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A stored photo of a recipe.</summary>
    /// <exception cref="NotFoundException">The recipe, or a stored photo with that id on it, doesn't exist.</exception>
    Task<RecipeImageContent> GetImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken);

    /// <summary>Adds an uploaded photo after the recipe's others. Like an edit, this makes an imported recipe a local one.</summary>
    /// <exception cref="RuleViolationException">The file isn't a JPEG, PNG, WebP or GIF image, is too large, or the recipe has too many photos.</exception>
    Task<RecipeResponse> AddImageAsync(Guid id, AddRecipeImageRequest request, CancellationToken cancellationToken);

    /// <summary>Removes a photo. Like an edit, this makes an imported recipe a local one.</summary>
    Task<RecipeResponse> RemoveImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the photos an import found for a recipe and keeps copies. A photo that can't be downloaded stays
    /// waiting, and the next import tries it again.
    /// </summary>
    /// <returns>What went wrong with each photo that couldn't be downloaded; empty when all of them worked.</returns>
    Task<IReadOnlyList<string>> FetchImagesAsync(FetchRecipeImages command, CancellationToken cancellationToken);

    /// <summary>The kinds of recipe file that can be imported.</summary>
    IReadOnlyList<RecipeFileFormatResponse> GetFileFormats();

    /// <summary>Adds the recipes in an uploaded file. Importing the same file again adds nothing new.</summary>
    Task<FileImportResult> ImportFileAsync(ImportFileRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the recipes in an uploaded archive, such as a Mealie backup .zip, with their photos. A recipe already in
    /// the library is left alone, except that one with no photos gets the archive's. Importing the same archive again
    /// adds nothing new.
    /// </summary>
    /// <param name="fileName">The archive's name, which decides the format.</param>
    /// <param name="archive">The archive's bytes. It must allow seeking.</param>
    /// <param name="cancellationToken">Stops the import.</param>
    /// <exception cref="RuleViolationException">The file isn't an archive ChefBot can read.</exception>
    Task<FileImportResult> ImportArchiveAsync(string fileName, Stream archive, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the recipe on a web page, read with the reader for its site (<see cref="WebImportResponse.ReadWith"/>
    /// names it), and queues its photos for download. Only public internet addresses are downloaded.
    /// </summary>
    /// <exception cref="RuleViolationException">The address isn't a web address, or the page has no recipe ChefBot can read.</exception>
    /// <exception cref="ConflictException">That page, or a recipe with the same name, is already in the library.</exception>
    /// <exception cref="RecipeSourceException">The page couldn't be downloaded.</exception>
    Task<WebImportResponse> ImportFromWebAsync(WebRecipeRequest request, CancellationToken cancellationToken);

    /// <summary>The setup wizard's sample recipes, marked when already in the library.</summary>
    Task<IReadOnlyList<SampleRecipeResponse>> GetSamplesAsync(CancellationToken cancellationToken);

    /// <summary>Adds the chosen samples. Names already in the library are skipped.</summary>
    Task<AddSamplesResponse> AddSamplesAsync(AddSamplesRequest request, CancellationToken cancellationToken);

    Task<MealieSettingsResponse> GetMealieSettingsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves how to reach Mealie. An empty address turns Mealie off and forgets the token. An empty token keeps
    /// the saved one, so the address can be changed without entering the token again.
    /// </summary>
    Task<MealieSettingsResponse> UpdateMealieSettingsAsync(MealieSettingsRequest request, CancellationToken cancellationToken);

    /// <summary>Uses the address and token from the configuration as the starting value, when none has been saved.</summary>
    Task UseDefaultMealieConnectionAsync(string? url, string? apiToken, CancellationToken cancellationToken);

    /// <summary>Queues a background Mealie import by sending <see cref="ImportMealieRecipes"/>.</summary>
    /// <exception cref="RuleViolationException">Mealie isn't set up.</exception>
    Task RequestMealieImportAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Copies recipes from Mealie, so planning never depends on Mealie being up. Publishes
    /// <see cref="RecipeImportFinished"/> whether it worked or not, and returns the same event.
    /// </summary>
    Task<RecipeImportFinished> ImportFromMealieAsync(ImportMealieRecipes command, CancellationToken cancellationToken);
}
