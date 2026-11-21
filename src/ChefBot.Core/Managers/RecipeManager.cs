using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Managers;

/// <inheritdoc cref="IRecipeManager"/>
internal sealed class RecipeManager(
    IRecipeAccess recipes,
    IImageSourceAccess images,
    IWebPageAccess web,
    ISampleRecipeAccess samples,
    IMessageAccess messages,
    TransformationEngine transformation,
    ValidationEngine validation,
    TimeProvider clock) : IRecipeManager
{
    public async Task<IReadOnlyList<RecipeResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await recipes.ListActiveAsync(cancellationToken)).Select(recipe => recipe.ToResponse()).ToList();

    public async Task<RecipeResponse> CreateAsync(RecipeDraft draft, CancellationToken cancellationToken)
    {
        var text = draft.ToText();
        ValidationEngine.Recipe(text);

        var recipe = new Recipe();
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return recipe.ToResponse();
    }

    public async Task<RecipeResponse> UpdateAsync(Guid id, RecipeDraft draft, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        var text = draft.ToText();
        ValidationEngine.Recipe(text);

        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return recipe.ToResponse();
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
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
        return recipe.ToResponse();
    }

    public async Task<RecipeResponse> RemoveImageAsync(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var recipe = await validation.RecipeAsync(id, cancellationToken);
        ValidationEngine.ImageOn(recipe, imageId);

        TransformationEngine.RemoveImage(recipe, imageId, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await messages.CommitAsync(cancellationToken);
        return recipe.ToResponse();
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
        var file = transformation.ReadFile(request.FileName ?? string.Empty, request.Content ?? string.Empty);
        var added = await AddNewAsync(file.Recipes, cancellationToken);
        return new FileImportResult(added, file.Recipes.Count - added, file.Skipped);
    }

    public async Task<FileImportResult> ImportArchiveAsync(string fileName, Stream archive, CancellationToken cancellationToken)
    {
        var file = transformation.ReadArchive(fileName ?? string.Empty, archive);
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

    public async Task<WebImportResponse> ImportFromWebAsync(WebRecipeRequest request, CancellationToken cancellationToken)
    {
        var url = ValidationEngine.RecipePageAddress(request.Url);
        var library = await recipes.ListAllAsync(cancellationToken);
        ValidationEngine.PageNotInLibrary(TransformationEngine.SourceAddress(url), library);

        var reader = transformation.PageReader(url);
        var page = await web.FetchAsync(reader.PageToFetch(url), cancellationToken);
        var text = transformation.ReadPage(reader, url, page);
        ValidationEngine.Recipe(text);
        ValidationEngine.NameNotInLibrary(text, library);

        var recipe = new Recipe();
        transformation.ApplyText(text, recipe, clock.GetUtcNow());
        await recipes.SaveAsync(recipe, cancellationToken);
        await RequestImagesAsync(recipe);
        await messages.CommitAsync(cancellationToken);
        return new WebImportResponse(recipe.ToResponse(), reader.Name);
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
}
