using ChefBot.Api.Messaging;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The recipe library kept in ChefBot's own database, and importing into it from a file, a web page, samples or Mealie.</summary>
public static class RecipeEndpoints
{
    /// <summary>The largest archive <see cref="ImportArchive"/> takes. Mealie backups with photos run to tens of MB.</summary>
    private const long MaxArchiveBytes = 200L * 1024 * 1024;

    [WolverineGet("/api/recipes")]
    public static Task<IReadOnlyList<RecipeResponse>> List([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ListAsync(cancellationToken);

    /// <summary>Adds a recipe. Ingredients are free-text lines such as "1 lb chicken thighs".</summary>
    [WolverinePost("/api/recipes")]
    public static Task<RecipeResponse> Create(RecipeDraft request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.CreateAsync(request, cancellationToken);

    /// <summary>Edits a recipe. An imported recipe becomes a local one, so imports leave the edit alone.</summary>
    [WolverinePut("/api/recipes/{id}")]
    public static Task<RecipeResponse> Update(
        RecipeDraft request, Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.UpdateAsync(id, request, cancellationToken);

    /// <summary>Archives a recipe so it is never planned again. Past plans keep showing it.</summary>
    [WolverineDelete("/api/recipes/{id}")]
    public static Task Archive(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ArchiveAsync(id, cancellationToken);

    /// <summary>
    /// A recipe photo, as an image file. A photo's bytes never change once stored (replacing one gives it a new
    /// id), so browsers may keep it for good.
    /// </summary>
    [WolverineGet("/api/recipes/{id}/images/{imageId}")]
    public static async Task<IResult> GetImage(
        Guid id, Guid imageId, HttpContext http, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken)
    {
        var image = await recipes.GetImageAsync(id, imageId, cancellationToken);
        http.Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return Results.File(image.Data, image.ContentType);
    }

    /// <summary>Adds a photo to a recipe: <c>{"data": "&lt;base64&gt;"}</c>, a JPEG, PNG, WebP or GIF of up to 5 MB.</summary>
    [WolverinePost("/api/recipes/{id}/images")]
    public static Task<RecipeResponse> AddImage(
        AddRecipeImageRequest request, Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.AddImageAsync(id, request, cancellationToken);

    /// <summary>Removes a photo from a recipe.</summary>
    [WolverineDelete("/api/recipes/{id}/images/{imageId}")]
    public static Task<RecipeResponse> RemoveImage(
        Guid id, Guid imageId, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RemoveImageAsync(id, imageId, cancellationToken);

    /// <summary>The kinds of recipe file that can be imported.</summary>
    [WolverineGet("/api/recipes/import/formats")]
    public static IReadOnlyList<RecipeFileFormatResponse> FileFormats([FromServices] IRecipeManager recipes) =>
        recipes.GetFileFormats();

    /// <summary>
    /// Adds the recipes in a file, read with the format its name ending belongs to. Recipes whose name is
    /// already in the library are left alone, so the same file can be imported again safely.
    /// </summary>
    [WolverinePost("/api/recipes/import/file")]
    public static Task<FileImportResult> ImportFile(
        ImportFileRequest request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ImportFileAsync(request, cancellationToken);

    /// <summary>
    /// Adds the recipes in an archive sent as the raw request body, with their photos: today a Mealie backup .zip
    /// (<c>Content-Type: application/zip</c>). <paramref name="fileName"/> decides the format. Recipes whose name is
    /// already in the library are left alone, except that one with no photos gets the archive's.
    /// </summary>
    [WolverinePost("/api/recipes/import/archive")]
    public static async Task<FileImportResult> ImportArchive(
        [FromQuery] string? fileName, HttpContext http, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken)
    {
        if (http.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
        {
            limit.MaxRequestBodySize = MaxArchiveBytes;
        }

        // A .zip is read by seeking to its end, which the request body can't do, so it is kept in a temporary file.
        await using var copy = new FileStream(
            Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose);
        await http.Request.Body.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;
        return await recipes.ImportArchiveAsync(fileName ?? string.Empty, copy, cancellationToken);
    }

    /// <summary>
    /// Adds the recipe on a web page: <c>{"url": "https://example.com/recipes/soup"}</c>. The page is read with the
    /// reader for its site, named in <c>readWith</c>, and its photos are downloaded in the background. Answers 409
    /// when that page or a recipe with the same name is already in the library, and 502 when the page can't be
    /// downloaded.
    /// </summary>
    [WolverinePost("/api/recipes/import/web")]
    public static Task<WebImportResponse> ImportFromWeb(
        WebRecipeRequest request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ImportFromWebAsync(request, cancellationToken);

    /// <summary>Starts a Mealie import in the background. Poll the status endpoint for the outcome.</summary>
    [WolverinePost("/api/recipes/import/mealie")]
    public static async Task<IResult> ImportFromMealie(
        [FromServices] IRecipeManager recipes,
        [FromServices] ImportStatusTracker status,
        [FromServices] TimeProvider clock,
        CancellationToken cancellationToken)
    {
        await recipes.RequestMealieImportAsync(cancellationToken);
        var queued = new ImportStatus("Running", clock.GetUtcNow());
        status.Set(queued);
        return Results.Accepted("/api/recipes/import/status", queued);
    }

    [WolverineGet("/api/recipes/import/status")]
    public static ImportStatus GetImportStatus([FromServices] ImportStatusTracker status) => status.Current;

    /// <summary>Setup wizard: the sample recipes, with their ingredients, marked when already in the library. Admins only.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/setup/samples")]
    public static Task<IReadOnlyList<SampleRecipeResponse>> Samples([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetSamplesAsync(cancellationToken);

    /// <summary>Setup wizard: adds the chosen samples to the library. Names already in the library are skipped. Admins only.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePost("/api/setup/samples")]
    public static Task<AddSamplesResponse> AddSamples(
        AddSamplesRequest request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.AddSamplesAsync(request, cancellationToken);

    /// <summary>How ChefBot reaches Mealie. The token is never sent back. Admins only.</summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverineGet("/api/system/mealie")]
    public static Task<MealieSettingsResponse> GetMealieSettings([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetMealieSettingsAsync(cancellationToken);

    /// <summary>
    /// Saves Mealie's address and API token. Leave the token empty to keep the saved one, or send an empty
    /// address to stop using Mealie. Mealie__Url and Mealie__ApiToken give the starting values. Admins only.
    /// </summary>
    [Authorize(Policy = Sessions.AdminPolicy)]
    [WolverinePut("/api/system/mealie")]
    public static Task<MealieSettingsResponse> UpdateMealieSettings(
        MealieSettingsRequest request, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.UpdateMealieSettingsAsync(request, cancellationToken);
}
