using System.Security.Claims;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>The recipe library kept in ChefBot's own database, and importing into it from a file, a web page or samples.</summary>
public static class RecipeEndpoints
{
    /// <summary>The largest archive <see cref="ImportArchive"/> takes. Mealie backups with photos run to tens of MB.</summary>
    private const long MaxArchiveBytes = 200L * 1024 * 1024;

    [WolverineGet("/api/recipes")]
    public static Task<IReadOnlyList<RecipeResponse>> List([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.ListAsync(cancellationToken);

    /// <summary>
    /// One page of the library, such as <c>/api/recipes/search?query=chicken&amp;course=Main&amp;page=2&amp;pageSize=25</c>.
    /// Every parameter is optional: no query matches every recipe, and the page size defaults to 25 (100 at most).
    /// <c>tag</c> may be given several times, and a recipe needs every one; <c>favourites=true</c> keeps anyone's
    /// favourites and <c>favouredBy</c> one person's; <c>slowCooker=true</c> and <c>readyWithin</c> (minutes) narrow further.
    /// </summary>
    [WolverineGet("/api/recipes/search")]
    public static Task<RecipePageResponse> Search(
        string? query,
        RecipeCourse? course,
        int? page,
        int? pageSize,
        bool? favourites,
        Guid? favouredBy,
        bool? slowCooker,
        int? readyWithin,
        HttpContext http,
        [FromServices] IRecipeManager recipes,
        CancellationToken cancellationToken) =>
        recipes.SearchAsync(
            new RecipeSearchRequest(
                query,
                course,
                page ?? 1,
                pageSize ?? 0,
                http.Request.Query["tag"].OfType<string>().ToList(),
                favourites ?? false,
                favouredBy,
                slowCooker ?? false,
                readyWithin),
            cancellationToken);

    /// <summary>What the library can be filtered by: every tag, and the people who have favourites.</summary>
    [WolverineGet("/api/recipes/filters")]
    public static Task<RecipeFiltersResponse> Filters([FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetFiltersAsync(cancellationToken);

    /// <summary>
    /// Every recipe that can be planned, as one file to download: schema.org JSON-LD unless <c>format</c> asks for
    /// <c>Pdf</c>, <c>Markdown</c> or <c>Text</c> (in any case). A format it doesn't know gives JSON-LD.
    /// </summary>
    [WolverineGet("/api/recipes/export")]
    public static async Task<IResult> ExportAll(
        RecipeExportFormat? format, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        Download(await recipes.ExportAllAsync(Format(format), cancellationToken));

    /// <summary>
    /// One recipe as a file to download: schema.org JSON-LD unless <c>format</c> asks for <c>Pdf</c>, <c>Markdown</c>
    /// or <c>Text</c>. An archived recipe is not found.
    /// </summary>
    [WolverineGet("/api/recipes/{id}/export")]
    public static async Task<IResult> Export(
        Guid id, RecipeExportFormat? format, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        Download(await recipes.ExportAsync(id, Format(format), cancellationToken));

    /// <summary>One recipe in full. An archived recipe is not found.</summary>
    [WolverineGet("/api/recipes/{id}")]
    public static Task<RecipeResponse> Get(Guid id, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.GetAsync(id, cancellationToken);

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
    /// Marks a recipe as one of the signed-in person's favourites. Gives back the recipe's summary, with everyone who
    /// has it as a favourite. Marking it again changes nothing.
    /// </summary>
    [WolverinePut("/api/recipes/{id}/favourite")]
    public static Task<RecipeSummaryResponse> AddFavourite(
        Guid id, ClaimsPrincipal principal, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.AddFavouriteAsync(id, Sessions.RequireUserId(principal), cancellationToken);

    /// <summary>Takes a recipe off the signed-in person's favourites, and gives back the recipe's summary.</summary>
    [WolverineDelete("/api/recipes/{id}/favourite")]
    public static Task<RecipeSummaryResponse> RemoveFavourite(
        Guid id, ClaimsPrincipal principal, [FromServices] IRecipeManager recipes, CancellationToken cancellationToken) =>
        recipes.RemoveFavouriteAsync(id, Sessions.RequireUserId(principal), cancellationToken);

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

    private static IResult Download(RecipeExportFile file) => Results.File(file.Data, file.ContentType, file.FileName);

    /// <summary>The format asked for; JSON-LD when none was, or when it is a number that names no format.</summary>
    private static RecipeExportFormat Format(RecipeExportFormat? format) =>
        format is { } asked && Enum.IsDefined(asked) ? asked : RecipeExportFormat.Json;
}
