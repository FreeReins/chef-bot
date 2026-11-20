using System.Globalization;
using System.Text.Json;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using static ChefBot.Core.Engines.Transformation.JsonValues;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// The <c>database.json</c> inside a Mealie backup .zip: Mealie's tables, one array of rows each. Recipes are in
/// <c>recipes</c>, their ingredient lines in <c>recipes_ingredients</c>, and their tags, categories and tools in
/// link tables, which feed the course classifier.
/// </summary>
internal sealed class MealieBackupFormat(RecipeClassifier classifier) : IRecipeFileFormat
{
    public string Name => "Mealie backup (database.json from the backup .zip)";

    public IReadOnlyList<string> Extensions { get; } = [".json"];

    /// <summary>Recognised by its ingredient table, which only a Mealie database dump has.</summary>
    public bool CanRead(string text) => text.Contains("\"recipes_ingredients\"", StringComparison.Ordinal);

    /// <exception cref="RuleViolationException">The file isn't JSON or holds no recipes.</exception>
    public RecipeFile Read(string text)
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
        var labels = Links(tables, "recipes_to_tags", "tag_id", Names(tables, "tags"))
            .Concat(Links(tables, "recipes_to_categories", "category_id", Names(tables, "categories")))
            .Concat(Links(tables, "recipes_to_tools", "tool_id", Names(tables, "tools")))
            .ToLookup(link => link.RecipeId, link => link.Label);

        var drafts = new List<RecipeText>();
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

            var (course, isSlowCooker) = classifier.Classify(name, labels[id]);
            var servings = PositiveAmount(Property(recipe, "recipe_servings"))
                ?? PositiveAmount(Property(recipe, "recipe_yield_quantity"))
                ?? PositiveAmount(Property(recipe, "recipe_yield"));
            drafts.Add(new RecipeText(name, course, lines, isSlowCooker, servings));
        }

        return new RecipeFile(drafts, recipes.Count - drafts.Count);
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

    /// <summary>An id column as text, whether Mealie wrote it as a string or a number. Empty when missing.</summary>
    private static string Id(JsonElement row, string column = "id") => Property(row, column) switch
    {
        { ValueKind: JsonValueKind.String } text => text.GetString() ?? string.Empty,
        { ValueKind: JsonValueKind.Number } number => number.GetRawText(),
        _ => string.Empty,
    };
}
