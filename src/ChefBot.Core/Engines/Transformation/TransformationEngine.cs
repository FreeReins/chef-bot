using System.Globalization;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Turns outside data into ChefBot's own: recipe files, typed-in recipes and Mealie recipes into stored recipes,
/// ingredient names into pantry items and free-form meals. Reads and saves nothing.
/// </summary>
internal sealed class TransformationEngine(
    IngredientCatalog catalog,
    PlannerOptions options,
    RecipeClassifier classifier,
    IEnumerable<IRecipeFileFormat> formats)
{
    /// <summary>Every kind of file that can be imported.</summary>
    public IReadOnlyList<IRecipeFileFormat> Formats { get; } = formats.ToList();

    /// <summary>Reads an uploaded recipe file with the format its name ending belongs to.</summary>
    /// <exception cref="RuleViolationException">The file is of a kind ChefBot can't read, or its contents can't be read.</exception>
    public RecipeFile ReadFile(string fileName, string text)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        var candidates = Formats.Where(f => f.Extensions.Contains(extension)).ToList();
        var format = candidates.FirstOrDefault(f => f.CanRead(text)) ?? candidates.LastOrDefault()
            ?? throw new RuleViolationException(
                $"ChefBot can't import {(extension.Length > 0 ? $"{extension} files" : "files without a type")} yet. " +
                $"Choose one of these instead: {string.Join(", ", Formats.Select(f => $"{f.Name} ({string.Join(", ", f.Extensions)})"))}.");

        return format.Read(text);
    }

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

    /// <summary>
    /// Works out what a Mealie import changes: new recipes are added, imported ones refreshed, ones edited in
    /// ChefBot left alone, and imported ones that Mealie no longer has archived. Only a complete read may archive:
    /// a partial one must not hide recipes that are still there.
    /// </summary>
    public ImportChanges MergeImport(RecipeSourceResult fetched, IReadOnlyList<Recipe> library, DateTimeOffset now)
    {
        var existing = library
            .Where(r => r.ExternalId is not null)
            .ToDictionary(r => r.ExternalId!, StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var changed = new List<Recipe>();
        int added = 0, updated = 0, keptLocal = 0, archived = 0;

        foreach (var incoming in fetched.Recipes.Where(r => seen.Add(r.ExternalId)))
        {
            if (!existing.TryGetValue(incoming.ExternalId, out var recipe))
            {
                recipe = new Recipe { Origin = RecipeOrigin.Mealie, ExternalId = incoming.ExternalId };
                added++;
            }
            else if (recipe.Origin == RecipeOrigin.Local)
            {
                keptLocal++;
                continue;
            }
            else
            {
                updated++;
            }

            ApplyImported(incoming, recipe, now);
            changed.Add(recipe);
        }

        if (fetched.Skipped == 0)
        {
            foreach (var missing in existing.Values.Where(r => r.Origin == RecipeOrigin.Mealie && !r.IsArchived && !seen.Contains(r.ExternalId!)))
            {
                missing.IsArchived = true;
                changed.Add(missing);
                archived++;
            }
        }

        return new ImportChanges(changed, new ImportResult(added, updated, keptLocal, archived, fetched.Skipped));
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

    private void ApplyImported(SourceRecipe incoming, Recipe recipe, DateTimeOffset now)
    {
        var (course, isSlowCooker) = classifier.Classify(incoming.Name, incoming.Labels);
        recipe.Name = incoming.Name.Trim();
        recipe.Course = course;
        recipe.IsSlowCooker = isSlowCooker;
        recipe.Servings = incoming.Servings is > 0m ? incoming.Servings.Value : options.DefaultServings;
        recipe.Description = Tidy(incoming.Description);
        recipe.Instructions = TidyList(incoming.Instructions);
        recipe.PrepMinutes = DurationParser.Minutes(incoming.PrepTime);
        recipe.CookMinutes = DurationParser.Minutes(incoming.CookTime);
        recipe.TotalMinutes = DurationParser.Minutes(incoming.TotalTime);
        recipe.SourceUrl = Tidy(incoming.SourceUrl);
        recipe.YieldText = Tidy(incoming.YieldText);
        recipe.Notes = Tidy(incoming.Notes);
        recipe.Tags = Tags(incoming.Labels);
        recipe.IsArchived = false;
        recipe.UpdatedAt = now;
        ReplaceIngredients(recipe, Number(incoming.Ingredients.Select(FromSourceRow).OfType<RecipeIngredient>()));
        ReplaceImageSources(recipe, incoming.ImageUrls ?? []);
    }

    /// <summary>
    /// Makes the recipe's downloaded photos match the source's list. A photo whose address is still listed keeps
    /// its stored bytes, new addresses wait to be downloaded, and ones the source dropped go. Uploaded photos stay.
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

    /// <summary>An ingredient from an outside source, using its parsed food when it has one.</summary>
    private RecipeIngredient? FromSourceRow(SourceIngredient row)
    {
        if (!string.IsNullOrWhiteSpace(row.FoodName))
        {
            var ingredient = Create(row.FoodName, row.Quantity > 0 ? row.Quantity : null, row.Unit);
            ingredient.Note = Tidy(row.Note);
            ingredient.Text = Tidy(row.Text) ?? Written(ingredient);
            return ingredient;
        }

        // Section headings and empty rows have no text at all.
        return string.IsNullOrWhiteSpace(row.Text) ? null : FromLine(row.Text);
    }

    /// <summary>A parsed ingredient written as a line: "1.5 lb chicken thigh, skin removed".</summary>
    private static string Written(RecipeIngredient ingredient)
    {
        var amount = ingredient.Quantity is decimal quantity ? quantity.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;
        var line = string.Join(' ', new[] { amount, ingredient.Unit, ingredient.DisplayName }.Where(part => part.Length > 0));
        return ingredient.Note is { } note ? $"{line}, {note}" : line;
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
