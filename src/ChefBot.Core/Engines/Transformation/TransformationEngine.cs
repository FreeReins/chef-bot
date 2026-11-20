using ChefBot.Core.Contracts;
using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>The recipes a Mealie import adds or changes, and the counts reported back.</summary>
internal sealed record ImportChanges(IReadOnlyList<Recipe> Changed, ImportResult Result);

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

    /// <summary>Fills a recipe in from its text. Ingredient rows are left alone when the lines didn't change.</summary>
    public void ApplyText(RecipeText text, Recipe recipe, DateTimeOffset now)
    {
        recipe.Name = text.Name.Trim();
        recipe.Course = text.Course;
        recipe.IsSlowCooker = text.IsSlowCooker;
        recipe.Servings = text.Servings ?? recipe.Servings;
        recipe.UpdatedAt = now;
        ReplaceIngredients(recipe, FromLines(text.Ingredients ?? []));
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
        recipe.IsArchived = false;
        recipe.UpdatedAt = now;
        ReplaceIngredients(recipe, Number(incoming.Ingredients.Select(FromSourceRow).OfType<RecipeIngredient>()));
    }

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

    private static (string, string?, string, decimal?, string) Signature(RecipeIngredient ingredient) =>
        (ingredient.Key, ingredient.CatalogName, ingredient.DisplayName, ingredient.Quantity, ingredient.Unit);

    private RecipeIngredient FromLine(string line)
    {
        var parsed = IngredientLineParser.Parse(line);
        return Create(parsed.Name, parsed.Quantity, parsed.Unit);
    }

    /// <summary>An ingredient from an outside source, using its parsed food when it has one.</summary>
    private RecipeIngredient? FromSourceRow(SourceIngredient row)
    {
        if (!string.IsNullOrWhiteSpace(row.FoodName))
        {
            return Create(row.FoodName, row.Quantity > 0 ? row.Quantity : null, row.Unit);
        }

        // Section headings and empty rows have no text at all.
        return string.IsNullOrWhiteSpace(row.Text) ? null : FromLine(row.Text);
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
