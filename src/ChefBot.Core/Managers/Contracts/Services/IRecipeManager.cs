using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Managers.Contracts.Services;

/// <summary>
/// The recipe library in ChefBot's own database, importing into it from files, web pages and samples, and the
/// household's food history.
/// </summary>
public interface IRecipeManager
{
    /// <summary>Recipes that can be planned, by name.</summary>
    Task<IReadOnlyList<RecipeResponse>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One page of the recipes that can be planned, in course order and then by name, keeping those whose name,
    /// ingredients or tags contain every word of the query. A page past the last comes back empty.
    /// </summary>
    Task<RecipePageResponse> SearchAsync(RecipeSearchRequest request, CancellationToken cancellationToken);

    /// <summary>One recipe that can be planned, in full.</summary>
    /// <exception cref="NotFoundException">There is no such recipe, or it has been archived.</exception>
    Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// One recipe as a file: schema.org JSON-LD for other recipe apps or another ChefBot, or a PDF, Markdown or text
    /// file to read and keep.
    /// </summary>
    /// <exception cref="NotFoundException">There is no such recipe, or it has been archived.</exception>
    Task<RecipeExportFile> ExportAsync(Guid id, RecipeExportFormat format, CancellationToken cancellationToken);

    /// <summary>Every recipe that can be planned, in name order, in one file of the given format.</summary>
    Task<RecipeExportFile> ExportAllAsync(RecipeExportFormat format, CancellationToken cancellationToken);

    Task<RecipeResponse> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken);

    /// <summary>Saves an edit.</summary>
    Task<RecipeResponse> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken);

    /// <summary>Stops a recipe from being planned. Past plans keep showing it.</summary>
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>A stored photo of a recipe.</summary>
    /// <exception cref="NotFoundException">The recipe, or a stored photo with that id on it, doesn't exist.</exception>
    Task<RecipeImageContent> GetImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken);

    /// <summary>Adds an uploaded photo after the recipe's others.</summary>
    /// <exception cref="RuleViolationException">The file isn't a JPEG, PNG, WebP or GIF image, is too large, or the recipe has too many photos.</exception>
    Task<RecipeResponse> AddImageAsync(Guid id, AddRecipeImageRequest request, CancellationToken cancellationToken);

    /// <summary>Removes a photo.</summary>
    Task<RecipeResponse> RemoveImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads the photos an import found for a recipe and keeps copies. Only public internet addresses are
    /// downloaded. A photo that can't be downloaded stays waiting.
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

    /// <summary>
    /// The household's food history, newest month first: recipes that joined or retired, what was cooked, with
    /// debuts, fan favourites, comebacks and milestone dinners picked out, big shopping hauls and people joining.
    /// </summary>
    Task<HistoryResponse> GetHistoryAsync(HistoryRequest request, CancellationToken cancellationToken);

    /// <summary>The setup wizard's sample recipes, marked when already in the library.</summary>
    Task<IReadOnlyList<SampleRecipeResponse>> GetSamplesAsync(CancellationToken cancellationToken);

    /// <summary>Adds the chosen samples. Names already in the library are skipped.</summary>
    Task<AddSamplesResponse> AddSamplesAsync(AddSamplesRequest request, CancellationToken cancellationToken);
}
