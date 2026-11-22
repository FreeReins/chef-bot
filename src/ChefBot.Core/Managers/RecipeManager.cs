using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IRecipeManager"/>
internal sealed partial class RecipeManager(
    IRecipeAccess recipes,
    IImageSourceAccess images,
    IWebPageAccess web,
    ISampleRecipeAccess samples,
    IHistoryAccess history,
    IFavouriteAccess favourites,
    IReviewAccess reviews,
    IPlanAccess plans,
    ISettingsAccess settings,
    IMessageAccess messages,
    TransformationEngine transformation,
    HistoryEngine storyteller,
    AchievementEngine achievements,
    ValidationEngine validation,
    TimeProvider clock) : IRecipeManager
{
    /// <summary>The page size used when a search asks for none.</summary>
    private const int DefaultPageSize = 25;

    private const int MaxPageSize = 100;

    public async Task<IReadOnlyList<RecipeResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var library = await recipes.ListActiveAsync(cancellationToken);
        var favoured = await FavouredByAsync(library, cancellationToken);
        var rated = await RatedAsync(library, cancellationToken);
        return library.Select(recipe => recipe.ToResponse(favoured(recipe), rated(recipe))).ToList();
    }

    public async Task<RecipePageResponse> SearchAsync(RecipeSearchRequest request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = request.PageSize < 1 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var words = (request.Query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IReadOnlyCollection<Guid>? onlyIds = null;
        if (request.Favourites || request.FavouredBy is not null)
        {
            onlyIds = (await favourites.ListAllAsync(cancellationToken))
                .Where(favourite => request.FavouredBy is null || favourite.UserId == request.FavouredBy)
                .Select(favourite => favourite.RecipeId)
                .ToHashSet();
        }

        var query = new RecipeQuery(
            words,
            request.Course,
            (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue),
            pageSize,
            (request.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList(),
            onlyIds,
            request.SlowCooker,
            request.ReadyWithin is > 0 ? request.ReadyWithin : null);

        var found = await recipes.SearchActiveAsync(query, cancellationToken);
        var librarySize = await recipes.CountActiveAsync(cancellationToken);
        var favoured = await FavouredByAsync(found.Recipes, cancellationToken);
        var rated = await RatedAsync(found.Recipes, cancellationToken);
        return new RecipePageResponse(
            found.Recipes.Select(recipe => recipe.ToSummary(favoured(recipe), rated(recipe))).ToList(), found.Total, page, pageSize, librarySize);
    }

    public async Task<RecipeFiltersResponse> GetFiltersAsync(CancellationToken cancellationToken)
    {
        var tags = (await recipes.ListActiveTagsAsync(cancellationToken))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .Select(tag => new RecipeTagResponse(
                tag.GroupBy(spelling => spelling, StringComparer.Ordinal).OrderByDescending(spelling => spelling.Count()).First().Key,
                tag.Count()))
            .OrderByDescending(tag => tag.Recipes)
            .ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var people = (await favourites.ListAllAsync(cancellationToken))
            .DistinctBy(favourite => favourite.UserId)
            .Select(favourite => new FavouriteResponse(favourite.UserId, favourite.UserName))
            .ToList();
        return new RecipeFiltersResponse(tags, people);
    }

    public async Task<RecipeResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await ResponseAsync(await ActiveAsync(id, cancellationToken), cancellationToken);

    public async Task<RecipeExportFile> ExportAsync(Guid id, RecipeExportFormat format, CancellationToken cancellationToken) =>
        TransformationEngine.ExportFile(await ActiveAsync(id, cancellationToken), format);

    public async Task<RecipeExportFile> ExportAllAsync(RecipeExportFormat format, CancellationToken cancellationToken) =>
        TransformationEngine.ExportFile(
            await recipes.ListActiveAsync(cancellationToken), DateOnly.FromDateTime(clock.GetLocalNow().DateTime), format);

    public async Task<RecipeResponse> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken)
    {
        var text = draft.ToText();
        ValidationEngine.Recipe(text);

        var recipe = new Recipe();
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await RecordAsync(FoodMomentKind.RecipeAdded, recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return recipe.ToResponse([], null);
    }

    public async Task<RecipeResponse> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        var text = draft.ToText();
        ValidationEngine.Recipe(text);

        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ResponseAsync(recipe, cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        if (!recipe.IsArchived)
        {
            await RecordAsync(FoodMomentKind.RecipeRetired, recipe, cancellationToken);
        }

        recipe.IsArchived = true;
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
    }

    public async Task<RecipeImageContent> GetImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var file = await recipes.GetImageFileAsync(id, imageId, cancellationToken)
            ?? throw new NotFoundException($"There is no stored photo with id {imageId} on that recipe.");
        return new RecipeImageContent(file.ContentType, file.Data);
    }

    public async Task<RecipeResponse> AddImageAsync(Guid id, AddRecipeImageRequest request, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        ValidationEngine.RoomForImage(recipe);
        var contentType = ValidationEngine.Image(request.Data);

        var image = TransformationEngine.AddUploadedImage(recipe, contentType, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await recipes.SaveImageFileAsync(new RecipeImageFile(image.Id, contentType, request.Data!), cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ResponseAsync(recipe, cancellationToken);
    }

    public async Task<RecipeResponse> RemoveImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        ValidationEngine.ImageOn(recipe, imageId);

        TransformationEngine.RemoveImage(recipe, imageId, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return await ResponseAsync(recipe, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> FetchImagesAsync(FetchRecipeImages command, CancellationToken cancellationToken)
    {
        var recipe = await recipes.FindAsync(command.RecipeId, cancellationToken);
        if (recipe is null)
        {
            return [];
        }

        var problems = new List<string>();
        foreach (var image in TransformationEngine.PendingImages(recipe))
        {
            try
            {
                var data = await images.DownloadAsync(image.SourceUrl!, ValidationEngine.MaxImageBytes, cancellationToken);
                image.ContentType = ValidationEngine.Image(data);
                await recipes.SaveImageFileAsync(new RecipeImageFile(image.Id, image.ContentType, data), cancellationToken);
            }
            catch (Exception exception) when (exception is RecipeSourceException or RuleViolationException)
            {
                problems.Add($"A photo of {recipe.Name} from {image.SourceUrl} wasn't kept: {exception.Message}");
            }
        }

        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return problems;
    }

    public IReadOnlyList<RecipeFileFormatResponse> GetFileFormats() =>
        transformation.Formats.Select(format => new RecipeFileFormatResponse(format.Name, format.Extensions)).ToList();

    public async Task<FileImportResult> ImportFileAsync(ImportFileRequest request, CancellationToken cancellationToken)
    {
        var file = transformation.ReadFile(request.FileName ?? string.Empty, request.Content ?? string.Empty, await WordsAsync(cancellationToken));
        var added = await AddNewAsync(file.Recipes, cancellationToken);
        return new FileImportResult(added, file.Recipes.Count - added, file.Skipped);
    }

    public async Task<FileImportResult> ImportArchiveAsync(string fileName, Stream archive, CancellationToken cancellationToken)
    {
        var file = transformation.ReadArchive(fileName ?? string.Empty, archive, await WordsAsync(cancellationToken));
        var photos = file.Photos ?? new Dictionary<string, byte[]>();

        // Recipes already in the library, such as ones imported before from database.json, get a photo only when
        // they have none, so photos someone added or removed stay as they are.
        var photosAdded = 0;
        foreach (var recipe in await recipes.ListActiveAsync(cancellationToken))
        {
            if (TransformationEngine.NeedsPhoto(recipe) && photos.TryGetValue(recipe.Name, out var data)
                && PhotoFile(recipe, data) is { } photo)
            {
                await recipes.SaveAsync(recipe, cancellationToken);
                await recipes.SaveImageFileAsync(photo, cancellationToken);
                photosAdded++;
            }
        }

        var (added, newPhotos) = await AddNewAsync(file.Recipes, photos, cancellationToken);
        return new FileImportResult(added, file.Recipes.Count - added, file.Skipped, photosAdded + newPhotos);
    }

    /// <summary>The household's words for sorting imported recipes into courses.</summary>
    private async Task<ClassifierWords> WordsAsync(CancellationToken cancellationToken) =>
        (await settings.GetAdvancedAsync(cancellationToken)).Words;

    public async Task<WebImportResponse> ImportFromWebAsync(WebRecipeRequest request, CancellationToken cancellationToken)
    {
        var url = ValidationEngine.RecipePageAddress(request.Url);
        var library = await recipes.ListAllAsync(cancellationToken);
        ValidationEngine.PageNotInLibrary(TransformationEngine.SourceAddress(url), library);

        var reader = transformation.PageReader(url);
        var page = await web.FetchAsync(reader.PageToFetch(url), cancellationToken);
        var text = transformation.ReadPage(reader, url, page, await WordsAsync(cancellationToken));
        ValidationEngine.Recipe(text);
        ValidationEngine.NameNotInLibrary(text, library);

        var recipe = new Recipe();
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await RecordAsync(FoodMomentKind.RecipeAdded, recipe, cancellationToken);
        await RequestImagesAsync(recipe);
        await messages.CommitAsync(cancellationToken);
        return new WebImportResponse(recipe.ToResponse([], null), reader.Name);
    }

    public async Task<IReadOnlyList<SampleRecipeResponse>> GetSamplesAsync(CancellationToken cancellationToken)
    {
        var library = await recipes.ListActiveAsync(cancellationToken);
        var existing = library.Select(recipe => recipe.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return samples.All()
            .Select(sample => new SampleRecipeResponse(
                sample.Name, sample.Course, sample.IsSlowCooker, sample.Servings, sample.Ingredients, existing.Contains(sample.Name)))
            .ToList();
    }

    public async Task<AddSamplesResponse> AddSamplesAsync(AddSamplesRequest request, CancellationToken cancellationToken)
    {
        var wanted = (request.Names ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var chosen = samples.All().Where(sample => wanted.Contains(sample.Name)).ToList();
        return new AddSamplesResponse(await AddNewAsync(chosen, cancellationToken));
    }

    public async Task<HistoryResponse> GetHistoryAsync(HistoryRequest request, CancellationToken cancellationToken) =>
        storyteller.Tell(
            await history.ListAsync(cancellationToken),
            await reviews.ListAllAsync(cancellationToken),
            await recipes.ListAllAsync(cancellationToken),
            request,
            clock.LocalTimeZone);

    /// <summary>Adds the recipes whose names aren't in the library yet.</summary>
    /// <returns>How many were added.</returns>
    private async Task<int> AddNewAsync(IReadOnlyList<RecipeText> candidates, CancellationToken cancellationToken) =>
        (await AddNewAsync(candidates, new Dictionary<string, byte[]>(), cancellationToken)).Added;

    /// <summary>
    /// Adds the recipes not in the library yet, with their photo from <paramref name="photos"/> (by recipe name) when
    /// it is one ChefBot can show, and commits. Returns how many recipes and photos were added.
    /// </summary>
    private async Task<(int Added, int Photos)> AddNewAsync(
        IReadOnlyList<RecipeText> candidates, IReadOnlyDictionary<string, byte[]> photos, CancellationToken cancellationToken)
    {
        var fresh = transformation.NotYetInLibrary(candidates, await recipes.ListAllAsync(cancellationToken));
        var photosAdded = 0;
        foreach (var text in fresh)
        {
            ValidationEngine.Recipe(text);
            var recipe = new Recipe();
            transformation.ApplyText(text, recipe, clock.GetUtcNow());
            var photo = photos.TryGetValue(recipe.Name, out var data) ? PhotoFile(recipe, data) : null;
            await recipes.SaveAsync(recipe, cancellationToken);
            await RecordAsync(FoodMomentKind.RecipeAdded, recipe, cancellationToken);
            if (photo is not null)
            {
                await recipes.SaveImageFileAsync(photo, cancellationToken);
                photosAdded++;
            }

            await RequestImagesAsync(recipe);
        }

        await messages.CommitAsync(cancellationToken);
        return (fresh.Count, photosAdded);
    }

    /// <summary>
    /// Puts an imported photo on the recipe, after its others, and returns its bytes to store once the recipe is
    /// saved. Null, leaving the recipe alone, when the file isn't an image ChefBot can show or the recipe is full.
    /// </summary>
    private RecipeImageFile? PhotoFile(Recipe recipe, byte[] data)
    {
        string contentType;
        try
        {
            ValidationEngine.RoomForImage(recipe);
            contentType = ValidationEngine.Image(data);
        }
        catch (RuleViolationException)
        {
            return null;
        }

        var image = TransformationEngine.AddUploadedImage(recipe, contentType, clock.GetUtcNow());
        return new RecipeImageFile(image.Id, contentType, data);
    }

    /// <summary>Asks for the recipe's photos to be downloaded once this change is saved, when it has any waiting.</summary>
    private async Task RequestImagesAsync(Recipe recipe)
    {
        if (TransformationEngine.PendingImages(recipe).Count > 0)
        {
            await messages.PublishAsync(new FetchRecipeImages(recipe.Id));
        }
    }

    /// <summary>Notes in the food history that something happened to a recipe today.</summary>
    private Task RecordAsync(FoodMomentKind kind, Recipe recipe, CancellationToken cancellationToken) =>
        history.AddAsync(
            new FoodMoment { OccurredOn = clock.Today(), OccurredAt = clock.GetUtcNow(), Kind = kind, RecipeId = recipe.Id, Title = recipe.Name },
            cancellationToken);

    /// <summary>A recipe in full, with who has it as a favourite and how it is rated.</summary>
    private async Task<RecipeResponse> ResponseAsync(Recipe recipe, CancellationToken cancellationToken) =>
        recipe.ToResponse((await FavouredByAsync([recipe], cancellationToken))(recipe), (await RatedAsync([recipe], cancellationToken))(recipe));

    /// <summary>A recipe that can be planned.</summary>
    /// <exception cref="NotFoundException">There is no such recipe, or it has been archived.</exception>
    private async Task<Recipe> ActiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        return recipe.IsArchived
            ? throw new NotFoundException("That recipe has been archived, so it is no longer in the library.")
            : recipe;
    }
}
