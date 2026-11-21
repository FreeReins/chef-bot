using AngleSharp.Html.Parser;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Turns outside data into ChefBot's own: recipe files, recipe web pages, typed-in recipes into stored recipes,
/// ingredient names into pantry items and free-form meals. It also writes recipes back out: as schema.org files, or
/// as PDF, Markdown or plain text to read.
/// Reads and saves nothing.
/// </summary>
internal sealed class TransformationEngine(
    IngredientCatalog catalog,
    IEnumerable<IRecipeFileFormat> formats,
    IEnumerable<IRecipePageReader> pageReaders)
{
    /// <summary>Every kind of file that can be imported.</summary>
    public IReadOnlyList<IRecipeFileFormat> Formats { get; } = formats.ToList();

    /// <summary>The ways of reading recipe web pages, site-specific ones first.</summary>
    public IReadOnlyList<IRecipePageReader> PageReaders { get; } = pageReaders.ToList();

    /// <summary>The reader for a recipe page: the first registered one that handles its address.</summary>
    /// <exception cref="RuleViolationException">No reader handles it, which only happens if none reads any site.</exception>
    public IRecipePageReader PageReader(Uri url) =>
        PageReaders.FirstOrDefault(reader => reader.Handles(url))
        ?? throw new RuleViolationException($"ChefBot can't read recipes from {url.Host}. Type the recipe in instead.");

    /// <summary>
    /// Reads the recipe on a downloaded page with the reader chosen for its address. The recipe's source is the
    /// address that was given, without any <c>#fragment</c>, and its photo addresses are made absolute.
    /// </summary>
    /// <exception cref="RuleViolationException">The reader found no recipe on the page.</exception>
    public RecipeText ReadPage(IRecipePageReader reader, Uri url, WebPage page)
    {
        var document = new HtmlParser().ParseDocument(page.Html);
        var recipe = reader.Read(url, document)
            ?? throw new RuleViolationException(
                "ChefBot couldn't find a recipe on that page. Check the link goes to one recipe, not a list of them, or type the recipe in instead.");

        var images = (recipe.ImageUrls ?? [])
            .Select(image => Uri.TryCreate(page.Address, image, out var absolute) ? absolute : null)
            .Where(image => image is not null && (image.Scheme == Uri.UriSchemeHttp || image.Scheme == Uri.UriSchemeHttps))
            .Select(image => image!.AbsoluteUri)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return recipe with { SourceUrl = SourceAddress(url), ImageUrls = images };
    }

    /// <summary>A page address as recipes keep it: absolute, without a <c>#fragment</c>.</summary>
    public static string SourceAddress(Uri url) => url.GetLeftPart(UriPartial.Query);

    /// <summary>Reads an uploaded recipe file with the format its name ending belongs to.</summary>
    /// <exception cref="RuleViolationException">The file is of a kind ChefBot can't read, or its contents can't be read.</exception>
    public RecipeFile ReadFile(string fileName, string text)
    {
        var extension = Extension(fileName);
        if (Formats.OfType<IRecipeArchiveFormat>().Any(f => f.ArchiveExtension == extension))
        {
            throw new RuleViolationException(
                $"A {extension} file has to be uploaded as it is, not as text. Import it from the Recipes tab.");
        }

        var candidates = Formats.Where(f => f.Extensions.Contains(extension)).ToList();
        var format = candidates.FirstOrDefault(f => f.CanRead(text)) ?? candidates.LastOrDefault()
            ?? throw CannotImport(extension);

        return format.Read(text);
    }

    /// <summary>Reads an uploaded archive, such as a Mealie backup .zip, with the format its name ending belongs to.</summary>
    /// <param name="fileName">The archive's name, which decides the format.</param>
    /// <param name="archive">The archive's bytes. It must allow seeking.</param>
    /// <exception cref="RuleViolationException">No format reads archives with that ending, or its contents can't be read.</exception>
    public RecipeFile ReadArchive(string fileName, Stream archive)
    {
        var extension = Extension(fileName);
        var format = Formats.OfType<IRecipeArchiveFormat>().FirstOrDefault(f => f.ArchiveExtension == extension)
            ?? throw CannotImport(extension);

        return format.ReadArchive(archive);
    }

    /// <summary>The media type of schema.org JSON-LD files.</summary>
    public const string JsonLdType = "application/ld+json";

    /// <summary>The media type of PDF files.</summary>
    public const string PdfType = "application/pdf";

    /// <summary>The media type of Markdown files.</summary>
    public const string MarkdownType = "text/markdown; charset=utf-8";

    /// <summary>The media type of plain text files.</summary>
    public const string TextType = "text/plain; charset=utf-8";

    /// <summary>One recipe as a file of the given format, named after it.</summary>
    public static RecipeExportFile ExportFile(Recipe recipe, RecipeExportFormat format)
    {
        var (extension, type) = FileType(format);
        var data = format == RecipeExportFormat.Json ? SchemaRecipeWriter.Write(recipe) : Readable([recipe], format);
        return new RecipeExportFile($"{FileSlug(recipe.Name)}.{extension}", type, data);
    }

    /// <summary>Several recipes, in name order, as one file of the given format named after the day.</summary>
    public static RecipeExportFile ExportFile(IEnumerable<Recipe> recipes, DateOnly today, RecipeExportFormat format)
    {
        var (extension, type) = FileType(format);
        var ordered = recipes.OrderBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var data = format == RecipeExportFormat.Json ? SchemaRecipeWriter.Write(ordered) : Readable(ordered, format);
        return new RecipeExportFile($"chefbot-recipes-{today:yyyy-MM-dd}.{extension}", type, data);
    }

    private static (string Extension, string Type) FileType(RecipeExportFormat format) => format switch
    {
        RecipeExportFormat.Json => ("json", JsonLdType),
        RecipeExportFormat.Pdf => ("pdf", PdfType),
        RecipeExportFormat.Markdown => ("md", MarkdownType),
        RecipeExportFormat.Text => ("txt", TextType),
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "There is no such export format."),
    };

    /// <summary>Recipes as a file to read rather than import.</summary>
    private static byte[] Readable(IReadOnlyList<Recipe> recipes, RecipeExportFormat format) => format switch
    {
        RecipeExportFormat.Pdf => RecipePdfWriter.Write(recipes),
        RecipeExportFormat.Markdown => RecipeMarkdownWriter.Write(recipes),
        _ => RecipeTextWriter.Write(recipes),
    };

    /// <summary>A name as a file name: "Slaw Fish Tacos!" is "slaw-fish-tacos". Letters in any alphabet stay.</summary>
    public static string FileSlug(string name)
    {
        var words = new string(name.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var slug = string.Join('-', words);
        slug = slug.Length > 60 ? slug[..60].TrimEnd('-') : slug;
        return slug.Length > 0 ? slug : "recipe";
    }

    /// <summary>Whether an imported photo may be added to a recipe already in the library: only when it has none.</summary>
    public static bool NeedsPhoto(Recipe recipe) => recipe.Images.Count == 0;

    /// <summary>The recipes whose names aren't in the library yet, each name once.</summary>
    public IReadOnlyList<RecipeText> NotYetInLibrary(IEnumerable<RecipeText> recipes, IEnumerable<Recipe> library)
    {
        var taken = library.Select(recipe => recipe.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return recipes.Where(recipe => taken.Add(recipe.Name?.Trim() ?? string.Empty)).ToList();
    }

    /// <summary>
    /// Fills a recipe in from its text. Ingredient rows are left alone when the lines didn't change, and photos are
    /// only touched when the text lists some to download (the recipe form never does).
    /// </summary>
    public void ApplyText(RecipeText text, Recipe recipe, DateTimeOffset now)
    {
        recipe.Name = text.Name.Trim();
        recipe.Course = text.Course;
        recipe.IsSlowCooker = text.IsSlowCooker;
        recipe.Servings = text.Servings ?? recipe.Servings;
        recipe.Description = Tidy(text.Description);
        recipe.Instructions = TidyList(text.Instructions);
        recipe.PrepMinutes = text.PrepMinutes;
        recipe.CookMinutes = text.CookMinutes;
        recipe.TotalMinutes = text.TotalMinutes;
        recipe.SourceUrl = Tidy(text.SourceUrl);
        recipe.Author = Tidy(text.Author);
        recipe.YieldText = Tidy(text.YieldText);
        recipe.Notes = Tidy(text.Notes);
        recipe.Tags = Tags(text.Tags);
        recipe.UpdatedAt = now;
        ReplaceIngredients(recipe, FromLines(text.Ingredients ?? []));
        if (text.ImageUrls is not null)
        {
            ReplaceImageSources(recipe, text.ImageUrls);
        }
    }

    /// <summary>The recipe's photos that are waiting to be downloaded.</summary>
    public static IReadOnlyList<RecipeImage> PendingImages(Recipe recipe) =>
        recipe.Images.Where(image => !image.IsStored && image.SourceUrl is not null).ToList();

    /// <summary>Adds an uploaded photo after the recipe's others, already stored.</summary>
    public static RecipeImage AddUploadedImage(Recipe recipe, string contentType, DateTimeOffset now)
    {
        var image = new RecipeImage { ContentType = contentType };
        recipe.Images.Add(image);
        NumberImages(recipe);
        recipe.UpdatedAt = now;
        return image;
    }

    /// <summary>Takes a photo off a recipe. Its stored bytes go with it.</summary>
    /// <returns>False when the recipe has no photo with that id.</returns>
    public static bool RemoveImage(Recipe recipe, Guid imageId, DateTimeOffset now)
    {
        if (recipe.Images.RemoveAll(image => image.Id == imageId) == 0) return false;
        NumberImages(recipe);
        recipe.UpdatedAt = now;
        return true;
    }

    /// <summary>A new pantry item, with its expiration estimated from the catalog's shelf life unless one is given.</summary>
    /// <param name="ingredient">What was bought.</param>
    /// <param name="quantity">How much, when known.</param>
    /// <param name="unit">The unit of <paramref name="quantity"/>.</param>
    /// <param name="purchasedOn">The purchase date.</param>
    /// <param name="expiresOn">The date on the package. When null, the catalog's shelf life is used.</param>
    public PantryItem ToPantryItem(IIngredientRef ingredient, decimal? quantity, string? unit, DateOnly purchasedOn, DateOnly? expiresOn)
    {
        var shelfLife = catalog.Get(ingredient.CatalogName)?.ShelfLifeDays;
        DateOnly? estimated = shelfLife is int days ? purchasedOn.AddDays(days) : null;

        return new PantryItem
        {
            Key = ingredient.Key,
            CatalogName = ingredient.CatalogName,
            DisplayName = ingredient.DisplayName,
            Quantity = quantity,
            Unit = Units.Normalize(unit),
            PurchasedOn = purchasedOn,
            ExpiresOn = expiresOn ?? estimated,
        };
    }

    /// <summary>A pantry item from a typed-in name such as "kale".</summary>
    public PantryItem ToPantryItem(string name, decimal? quantity, string? unit, DateOnly purchasedOn, DateOnly? expiresOn) =>
        ToPantryItem(catalog.Identify(name), quantity, unit, purchasedOn, expiresOn);

    /// <summary>The ingredients of a free-form meal, from their names. Blank names and repeats are dropped.</summary>
    public List<PlannedMealIngredient> ToMealIngredients(IEnumerable<string>? names) =>
        (names ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => catalog.Identify(name))
            .DistinctBy(identity => identity.Key)
            .Select(identity => new PlannedMealIngredient
            {
                Key = identity.Key,
                CatalogName = identity.CatalogName,
                DisplayName = identity.DisplayName,
            })
            .ToList();

    /// <summary>Builds ingredients from free-text lines such as "1 lb chicken thighs". Blank lines are skipped.</summary>
    public List<RecipeIngredient> FromLines(IEnumerable<string> lines) =>
        Number(lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(FromLine));

    /// <summary>
    /// Makes the recipe's downloaded photos match the given list. A photo whose address is still listed keeps
    /// its stored bytes, new addresses wait to be downloaded, and ones no longer listed go. Uploaded photos stay.
    /// </summary>
    private static void ReplaceImageSources(Recipe recipe, IEnumerable<string> urls)
    {
        var wanted = urls
            .Select(url => url?.Trim() ?? string.Empty)
            .Where(WebAddress.IsHttp)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var existing = recipe.Images.Where(image => image.SourceUrl is not null).ToDictionary(image => image.SourceUrl!, StringComparer.Ordinal);
        var uploaded = recipe.Images.Where(image => image.SourceUrl is null);
        var downloaded = wanted.Select(url => existing.GetValueOrDefault(url) ?? new RecipeImage { SourceUrl = url });

        recipe.Images = [.. downloaded, .. uploaded];
        NumberImages(recipe);
    }

    private static string Extension(string? fileName) => Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

    private RuleViolationException CannotImport(string extension) => new(
        $"ChefBot can't import {(extension.Length > 0 ? $"{extension} files" : "files without a type")} yet. " +
        $"Choose one of these instead: {string.Join(", ", Formats.Select(f => $"{f.Name} ({string.Join(", ", f.Extensions)})"))}.");

    private static void NumberImages(Recipe recipe)
    {
        for (var position = 0; position < recipe.Images.Count; position++)
        {
            recipe.Images[position].Position = position;
        }
    }

    private static string? Tidy(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static List<string> TidyList(IEnumerable<string>? lines) =>
        (lines ?? []).Select(line => line?.Trim() ?? string.Empty).Where(line => line.Length > 0).ToList();

    private static List<string> Tags(IEnumerable<string>? tags) => TidyList(tags).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>Swaps in a new ingredient list, leaving the stored rows alone when nothing changed.</summary>
    private static void ReplaceIngredients(Recipe recipe, IReadOnlyList<RecipeIngredient> fresh)
    {
        var current = recipe.Ingredients.OrderBy(i => i.Position).Select(Signature);
        if (current.SequenceEqual(fresh.OrderBy(i => i.Position).Select(Signature)))
        {
            return;
        }

        recipe.Ingredients.Clear();
        recipe.Ingredients.AddRange(fresh);
    }

    private static (string, string?, string, decimal?, string, string, string?) Signature(RecipeIngredient ingredient) =>
        (ingredient.Key, ingredient.CatalogName, ingredient.DisplayName, ingredient.Quantity, ingredient.Unit, ingredient.Text, ingredient.Note);

    private RecipeIngredient FromLine(string line)
    {
        var parsed = IngredientLineParser.Parse(line);
        var ingredient = Create(parsed.Name, parsed.Quantity, parsed.Unit);
        ingredient.Text = line.Trim();
        return ingredient;
    }

    private RecipeIngredient Create(string name, decimal? quantity, string? unit)
    {
        var identity = catalog.Identify(name);
        return new RecipeIngredient
        {
            Key = identity.Key,
            CatalogName = identity.CatalogName,
            DisplayName = identity.DisplayName,
            Quantity = quantity,
            Unit = Units.Normalize(unit),
        };
    }

    private static List<RecipeIngredient> Number(IEnumerable<RecipeIngredient> ingredients)
    {
        var list = ingredients.ToList();
        for (var position = 0; position < list.Count; position++)
        {
            list[position].Position = position;
        }

        return list;
    }
}
