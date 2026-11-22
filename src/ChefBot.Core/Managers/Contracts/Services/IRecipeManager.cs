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
    /// ingredients or tags contain every word of the query and that pass every filter asked for. A page past the last
    /// comes back empty.
    /// </summary>
    Task<RecipePageResponse> SearchAsync(RecipeSearchRequest request, CancellationToken cancellationToken);

    /// <summary>What the library can be filtered by: its tags, and the people who have favourites.</summary>
    Task<RecipeFiltersResponse> GetFiltersAsync(CancellationToken cancellationToken);

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

    /// <summary>Marks a recipe as one of a person's favourites. Marking it again changes nothing.</summary>
    /// <returns>The recipe, with everyone who now has it as a favourite.</returns>
    /// <exception cref="NotFoundException">There is no such recipe or person, or the recipe has been archived.</exception>
    Task<RecipeSummaryResponse> AddFavouriteAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Takes a recipe off a person's favourites. Taking off one that isn't there changes nothing.</summary>
    /// <returns>The recipe, with everyone who still has it as a favourite.</returns>
    /// <exception cref="NotFoundException">There is no such recipe, or it has been archived.</exception>
    Task<RecipeSummaryResponse> RemoveFavouriteAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// The household's kitchen achievements: the first steps, badges with bronze, silver and gold tiers, and the
    /// points and chef rank they add up to, with what <paramref name="userId"/> hasn't been shown yet.
    /// </summary>
    Task<AchievementsResponse> GetAchievementsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Remembers that <paramref name="userId"/> was shown the new achievements up to one of them.</summary>
    Task MarkAchievementsSeenAsync(Guid userId, AchievementsSeenRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the achievement tiers and ranks reached since the last check, and announces them
    /// (<see cref="AchievementsUnlocked"/>). The first check ever records what was already reached without announcing
    /// it. Safe to run any number of times: what was saved is never saved again. Returns what was announced.
    /// </summary>
    /// <param name="causedBy">The message that asked for the check, kept with what it found.</param>
    /// <param name="cancellationToken">Stops the check.</param>
    Task<IReadOnlyList<AchievementUnlockResponse>> CheckAchievementsAsync(Guid? causedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Writes what an event means for the food history, and announces it (<see cref="HistoryRecorded"/>) when the
    /// history changed. Safe to run more than once for the same message: it adds nothing the second time.
    /// </summary>
    /// <param name="moment">The event, such as a meal cooked or recipes added.</param>
    /// <param name="sourceId">The message that carried it, kept with what it records.</param>
    /// <param name="cancellationToken">Stops the work.</param>
    Task RecordHistoryAsync(IKitchenMoment moment, Guid sourceId, CancellationToken cancellationToken);

    /// <summary>A recipe's reviews, newest first, with the rating they add up to.</summary>
    /// <exception cref="NotFoundException">There is no such recipe, or it has been archived.</exception>
    Task<RecipeReviewsResponse> ListReviewsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds a person's review of a recipe. A person can review a recipe as often as they like.</summary>
    /// <returns>The recipe's reviews and rating, with the new review.</returns>
    /// <exception cref="RuleViolationException">The stars aren't from 1 to 5, or the notes are too long.</exception>
    /// <exception cref="NotFoundException">There is no such recipe or person, or the recipe has been archived.</exception>
    Task<RecipeReviewsResponse> AddReviewAsync(Guid id, Guid userId, RecipeReviewRequest request, CancellationToken cancellationToken);

    /// <summary>Changes the stars and notes of a person's own review.</summary>
    /// <returns>The recipe's reviews and rating, with the change.</returns>
    /// <exception cref="RuleViolationException">The stars aren't from 1 to 5, or the notes are too long.</exception>
    /// <exception cref="NotAllowedException">The review is someone else's.</exception>
    /// <exception cref="NotFoundException">There is no such recipe or review, or the recipe has been archived.</exception>
    Task<RecipeReviewsResponse> UpdateReviewAsync(
        Guid id, Guid reviewId, Guid userId, RecipeReviewRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a review.</summary>
    /// <param name="id">The recipe.</param>
    /// <param name="reviewId">The review to delete.</param>
    /// <param name="userId">Who is deleting it.</param>
    /// <param name="mayRemoveAnyone">True when they may delete anyone's review, not only their own.</param>
    /// <param name="cancellationToken">Stops the request.</param>
    /// <returns>The recipe's reviews and rating, without it.</returns>
    /// <exception cref="NotAllowedException">The review is someone else's, and <paramref name="mayRemoveAnyone"/> is false.</exception>
    /// <exception cref="NotFoundException">There is no such recipe or review, or the recipe has been archived.</exception>
    Task<RecipeReviewsResponse> RemoveReviewAsync(
        Guid id, Guid reviewId, Guid userId, bool mayRemoveAnyone, CancellationToken cancellationToken);

    /// <summary>
    /// The household's food history, newest month first: recipes that joined or retired, what was cooked, with
    /// debuts, fan favourites, comebacks and milestone dinners picked out, reviews, big shopping hauls and people joining.
    /// </summary>
    Task<HistoryResponse> GetHistoryAsync(HistoryRequest request, CancellationToken cancellationToken);

    /// <summary>The setup wizard's sample recipes, marked when already in the library.</summary>
    Task<IReadOnlyList<SampleRecipeResponse>> GetSamplesAsync(CancellationToken cancellationToken);

    /// <summary>Adds the chosen samples. Names already in the library are skipped.</summary>
    Task<AddSamplesResponse> AddSamplesAsync(AddSamplesRequest request, CancellationToken cancellationToken);
}
