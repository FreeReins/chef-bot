using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using static ChefBot.Core.Engines.Transformation.JsonValues;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// A Mealie backup: the .zip Mealie makes, or the <c>database.json</c> inside it. That file holds Mealie's tables, one
/// array of rows each. Recipes are in <c>recipes</c>, their ingredient lines in <c>recipes_ingredients</c>, and their
/// tags, categories and tools in link tables, which feed the course classifier and become the recipe's tags. Steps are
/// in <c>recipe_instructions</c> and notes in <c>notes</c>. Photos are files beside it in the .zip, at
/// <c>data/recipes/{recipe id}/images/original.webp</c>, so only the .zip brings them.
/// </summary>
internal sealed class MealieBackupFormat : IRecipeArchiveFormat
{
    /// <summary>The largest <c>database.json</c> read from a .zip, so a crafted archive can't fill the memory.</summary>
    private const int MaxDatabaseBytes = 256 * 1024 * 1024;

    public string Name => "Mealie backup (the .zip, or database.json from it)";

    public IReadOnlyList<string> Extensions { get; } = [".json", ".zip"];

    public string ArchiveExtension => ".zip";

    /// <summary>Recognised by its ingredient table, which only a Mealie database dump has.</summary>
    public bool CanRead(string text) => text.Contains("\"recipes_ingredients\"", StringComparison.Ordinal);

    /// <exception cref="RuleViolationException">The file isn't JSON or holds no recipes.</exception>
    public RecipeFile Read(string text, ClassifierWords words) => Read(text, new Dictionary<Guid, byte[]>(), words);

    /// <exception cref="RuleViolationException">The .zip has no database.json, or it can't be read.</exception>
    public RecipeFile ReadArchive(Stream archive, ClassifierWords words)
    {
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(archive, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            throw new RuleViolationException("That file isn't a .zip file. Make a new backup in Mealie and import the .zip it gives you.");
        }

        using (zip)
        {
            var database = zip.GetEntry("database.json")
                ?? throw new RuleViolationException("That .zip has no database.json in it, so it isn't a Mealie backup. Make a new backup in Mealie and import the .zip it gives you.");
            var json = Contents(database, MaxDatabaseBytes)
                ?? throw new RuleViolationException("That Mealie backup is too large to import.");

            var photos = new Dictionary<Guid, byte[]>();
            foreach (var entry in zip.Entries)
            {
                if (PhotoOf(entry.FullName) is { } recipeId && !photos.ContainsKey(recipeId)
                    && Contents(entry, ValidationEngine.MaxImageBytes) is { } data)
                {
                    photos[recipeId] = data;
                }
            }

            return Read(Encoding.UTF8.GetString(json), photos, words);
        }
    }

    private static RecipeFile Read(string text, IReadOnlyDictionary<Guid, byte[]> photos, ClassifierWords words)
    {
        using var document = Parse(text, "Mealie backup");
        var tables = document.RootElement;

        var recipes = Rows(tables, "recipes").ToList();
        if (recipes.Count == 0)
        {
            throw new RuleViolationException("That Mealie backup has no recipes in it. Make a new backup in Mealie and try again.");
        }

        var foods = Names(tables, "ingredient_foods");
        var units = Rows(tables, "ingredient_units").GroupBy(row => Id(row)).ToDictionary(group => group.Key, group => Unit(group.First()));
        var ingredients = Rows(tables, "recipes_ingredients")
            .ToLookup(row => Id(row, "recipe_id"));
        var steps = Rows(tables, "recipe_instructions").ToLookup(row => Id(row, "recipe_id"));
        var notes = Rows(tables, "notes").ToLookup(row => Id(row, "recipe_id"));
        var labels = Links(tables, "recipes_to_tags", "tag_id", Names(tables, "tags"))
            .Concat(Links(tables, "recipes_to_categories", "category_id", Names(tables, "categories")))
            .Concat(Links(tables, "recipes_to_tools", "tool_id", Names(tables, "tools")))
            .ToLookup(link => link.RecipeId, link => link.Label);

        var drafts = new List<RecipeText>();
        var named = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var recipe in recipes)
        {
            var id = Id(recipe);
            var name = Text(Property(recipe, "name"))?.Trim();
            var lines = ingredients[id]
                .OrderBy(row => PositiveAmount(Property(row, "position")) ?? 0m)
                .Select(row => Line(row, foods, units))
                .OfType<string>()
                .ToList();
            if (string.IsNullOrEmpty(name) || lines.Count == 0) continue;

            if (Guid.TryParse(id, out var guid) && photos.TryGetValue(guid, out var photo))
            {
                named.TryAdd(name, photo);
            }

            var (course, isSlowCooker) = RecipeClassifier.Classify(name, labels[id], words);
            var servings = PositiveAmount(Property(recipe, "recipe_servings"))
                ?? PositiveAmount(Property(recipe, "recipe_yield_quantity"))
                ?? PositiveAmount(Property(recipe, "recipe_yield"));
            var tags = labels[id].Select(label => label.Trim()).Where(label => label.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            drafts.Add(new RecipeText(
                name,
                course,
                lines,
                isSlowCooker,
                servings,
                Description: Tidy(Text(Property(recipe, "description"))),
                Instructions: steps[id]
                    .OrderBy(row => PositiveAmount(Property(row, "position")) ?? 0m)
                    .Select(row => Tidy(Text(Property(row, "text"))))
                    .OfType<string>()
                    .ToList(),
                PrepMinutes: Minutes(Property(recipe, "prep_time")),
                CookMinutes: Minutes(Property(recipe, "perform_time") ?? Property(recipe, "cook_time")),
                TotalMinutes: Minutes(Property(recipe, "total_time")),
                SourceUrl: Tidy(Text(Property(recipe, "org_url"))),
                YieldText: YieldWords(Text(Property(recipe, "recipe_yield"))),
                Notes: Tidy(string.Join("\n\n", notes[id].Select(Note).Where(note => note.Length > 0))),
                Tags: tags));
        }

        return new RecipeFile(drafts, recipes.Count - drafts.Count, named);
    }

    /// <summary>
    /// One ingredient as a line of text. Mealie keeps unparsed lines in <c>note</c>; parsed ones have a food, an
    /// amount and a unit. Section headings have only a title and are left out.
    /// </summary>
    private static string? Line(JsonElement row, IReadOnlyDictionary<string, string> foods, IReadOnlyDictionary<string, string> units)
    {
        if (Text(Property(row, "original_text")) is { } original && !string.IsNullOrWhiteSpace(original)) return original.Trim();

        var note = Text(Property(row, "note"))?.Trim();
        if (!foods.TryGetValue(Id(row, "food_id"), out var food)) return string.IsNullOrEmpty(note) ? null : note;

        var quantity = PositiveAmount(Property(row, "quantity"));
        var unit = units.GetValueOrDefault(Id(row, "unit_id"));
        var parts = new[] { quantity?.ToString("0.###", CultureInfo.InvariantCulture), unit, food };
        return string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>A note's title and text, as "Title: text".</summary>
    private static string Note(JsonElement row) =>
        string.Join(": ", new[] { Tidy(Text(Property(row, "title"))), Tidy(Text(Property(row, "text"))) }.OfType<string>());

    private static string Unit(JsonElement row) =>
        Text(Property(row, "abbreviation")) is { Length: > 0 } abbreviation && Property(row, "use_abbreviation") is { ValueKind: JsonValueKind.True }
            ? abbreviation
            : Text(Property(row, "name")) ?? string.Empty;

    private static Dictionary<string, string> Names(JsonElement tables, string table) =>
        Rows(tables, table)
            .Where(row => !string.IsNullOrWhiteSpace(Text(Property(row, "name"))))
            .GroupBy(row => Id(row))
            .ToDictionary(group => group.Key, group => Text(Property(group.First(), "name"))!);

    private static IEnumerable<(string RecipeId, string Label)> Links(
        JsonElement tables, string table, string column, IReadOnlyDictionary<string, string> names) =>
        Rows(tables, table)
            .Select(row => (RecipeId: Id(row, "recipe_id"), Label: names.GetValueOrDefault(Id(row, column))))
            .Where(link => link.Label is not null)
            .Select(link => (link.RecipeId, link.Label!));

    /// <summary>
    /// The recipe a .zip entry is the photo of, for <c>data/recipes/{id}/images/original.*</c>. The smaller
    /// <c>min-</c> and <c>tiny-</c> copies beside it are left out. Ids are compared as GUIDs, with or without dashes.
    /// </summary>
    private static Guid? PhotoOf(string path)
    {
        var parts = path.Replace('\\', '/').Split('/');
        return parts is ["data", "recipes", var id, "images", var file]
            && Path.GetFileNameWithoutExtension(file).Equals("original", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(id, out var recipeId)
                ? recipeId
                : null;
    }

    /// <summary>
    /// An entry's bytes, or null when it is larger than <paramref name="maxBytes"/>. The size is counted while reading,
    /// not taken from the .zip's own record, which a crafted file could get wrong.
    /// </summary>
    private static byte[]? Contents(ZipArchiveEntry entry, int maxBytes)
    {
        if (entry.Length > maxBytes) return null;

        using var stream = entry.Open();
        using var copy = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer)) > 0)
        {
            if (copy.Length + read > maxBytes) return null;
            copy.Write(buffer, 0, read);
        }

        return copy.ToArray();
    }

    /// <summary>An id column as text, whether Mealie wrote it as a string or a number. Empty when missing.</summary>
    private static string Id(JsonElement row, string column = "id") => Property(row, column) switch
    {
        { ValueKind: JsonValueKind.String } text => text.GetString() ?? string.Empty,
        { ValueKind: JsonValueKind.Number } number => number.GetRawText(),
        _ => string.Empty,
    };
}
