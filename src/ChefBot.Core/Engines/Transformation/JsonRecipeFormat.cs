using System.Text.Json;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Contracts.Services;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using static ChefBot.Core.Engines.Transformation.JsonValues;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// The general recipe JSON format. It accepts ChefBot's own shape (<c>name</c>, <c>course</c>,
/// <c>ingredients</c>, <c>servings</c>, <c>isSlowCooker</c>, <c>description</c>, <c>instructions</c>,
/// <c>prepTime</c>, <c>cookTime</c>, <c>totalTime</c>, <c>sourceUrl</c>, <c>author</c>, <c>yield</c>, <c>notes</c>,
/// <c>tags</c>, <c>images</c>) and schema.org recipes as recipe sites and Mealie's recipe export give them
/// (<c>recipeIngredient</c>, <c>recipeInstructions</c>, <c>recipeYield</c>, <c>recipeCategory</c>, <c>keywords</c>,
/// <c>image</c>, <c>url</c>, ISO 8601 times such as <c>PT30M</c>). Times may also be minutes or words.
/// The file may hold one recipe, a list of them, <c>{ "recipes": [...] }</c> or a JSON-LD <c>@graph</c>.
/// </summary>
internal sealed class JsonRecipeFormat(RecipeClassifier classifier) : IRecipeFileFormat
{
    private static readonly string[] IngredientTextFields = ["display", "originalText", "note", "text", "name"];
    private static readonly string[] LabelFields = ["tags", "recipeCategory", "categories", "keywords", "tools"];

    public string Name => "Recipe JSON (ChefBot, a Mealie recipe export or schema.org)";

    public IReadOnlyList<string> Extensions { get; } = [".json"];

    /// <summary>Always true: this is the fallback for any .json file no more specific format recognises.</summary>
    public bool CanRead(string text) => true;

    /// <exception cref="RuleViolationException">The file isn't JSON or holds no recipes at all.</exception>
    public RecipeFile Read(string text)
    {
        using var document = Parse(text, "JSON recipe");
        var entries = Entries(document.RootElement).ToList();
        if (entries.Count == 0)
        {
            throw new RuleViolationException("No recipes were found in that file. Each recipe needs a name and a list of ingredients.");
        }

        var recipes = entries.Select(ToDraft).OfType<RecipeText>().ToList();
        return new RecipeFile(recipes, entries.Count - recipes.Count);
    }

    /// <summary>The objects in the file that look like recipes.</summary>
    private static IEnumerable<JsonElement> Entries(JsonElement root) => root.ValueKind switch
    {
        JsonValueKind.Array => root.EnumerateArray().SelectMany(Entries),
        JsonValueKind.Object when Property(root, "recipes") is { ValueKind: JsonValueKind.Array } list => Entries(list),
        JsonValueKind.Object when Property(root, "@graph") is { ValueKind: JsonValueKind.Array } graph =>
            graph.EnumerateArray().Where(IsSchemaRecipe),
        JsonValueKind.Object when Property(root, "@type") is not null && !IsSchemaRecipe(root) => [],
        JsonValueKind.Object => [root],
        _ => [],
    };

    private static bool IsSchemaRecipe(JsonElement element) =>
        Property(element, "@type") is { } type && Strings(type).Any(t => t.Equals("Recipe", StringComparison.OrdinalIgnoreCase));

    private RecipeText? ToDraft(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object) return null;

        var name = Text(Property(entry, "name"))?.Trim();
        var ingredients = (Property(entry, "ingredients") ?? Property(entry, "recipeIngredient")) is { ValueKind: JsonValueKind.Array } list
            ? list.EnumerateArray().Select(IngredientLine).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line!.Trim()).ToList()
            : [];
        if (string.IsNullOrEmpty(name) || ingredients.Count == 0) return null;

        var labels = LabelFields.Select(field => Property(entry, field)).OfType<JsonElement>().SelectMany(Labels).ToList();
        var (course, isSlowCooker) = classifier.Classify(name, labels);
        if (Text(Property(entry, "course")) is { } written && Enum.TryParse<RecipeCourse>(written.Trim(), ignoreCase: true, out var chosen))
        {
            course = chosen;
        }

        if (Property(entry, "isSlowCooker") is { ValueKind: JsonValueKind.True or JsonValueKind.False } flag)
        {
            isSlowCooker = flag.GetBoolean();
        }

        var yieldValue = Property(entry, "yield") ?? Property(entry, "recipeYield");
        var servings = PositiveAmount(Property(entry, "servings") ?? Property(entry, "recipeServings") ?? yieldValue);
        return new RecipeText(
            name,
            course,
            ingredients,
            isSlowCooker,
            servings,
            Description: Tidy(Text(Property(entry, "description"))),
            Instructions: TextList(Property(entry, "instructions") ?? Property(entry, "recipeInstructions")),
            PrepMinutes: Minutes(Property(entry, "prepTime")),
            CookMinutes: Minutes(Property(entry, "cookTime") ?? Property(entry, "performTime")),
            TotalMinutes: Minutes(Property(entry, "totalTime")),
            SourceUrl: Tidy(Text(Property(entry, "sourceUrl") ?? Property(entry, "orgURL") ?? Property(entry, "url"))),
            Author: NameList(Property(entry, "author")),
            YieldText: YieldText(yieldValue),
            Notes: Notes(Property(entry, "notes")),
            Tags: labels.Select(label => label.Trim()).Where(label => label.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            ImageUrls: ImageUrls(Property(entry, "images") ?? Property(entry, "image")));
    }

    /// <summary>The yield's wording, from text or the last text in a list such as <c>["4", "4 bowls"]</c>.</summary>
    private static string? YieldText(JsonElement? value)
    {
        var text = value switch
        {
            { ValueKind: JsonValueKind.String } words => words.GetString(),
            { ValueKind: JsonValueKind.Array } list => list.EnumerateArray().Select(item => Text(item)).LastOrDefault(item => !string.IsNullOrWhiteSpace(item)),
            _ => null,
        };
        return YieldWords(text);
    }

    /// <summary>Notes as one text: a string, or Mealie's list of <c>{ title, text }</c>, one note per paragraph.</summary>
    private static string? Notes(JsonElement? value) => value switch
    {
        { ValueKind: JsonValueKind.String } text => Tidy(text.GetString()),
        { ValueKind: JsonValueKind.Array } list => Tidy(string.Join("\n\n", list.EnumerateArray().Select(Note).Where(note => note.Length > 0))),
        _ => null,
    };

    private static string Note(JsonElement note) => note.ValueKind switch
    {
        JsonValueKind.String => note.GetString()?.Trim() ?? string.Empty,
        JsonValueKind.Object => string.Join(": ", new[] { Text(Property(note, "title")), Text(Property(note, "text")) }
            .Select(part => part?.Trim()).Where(part => !string.IsNullOrEmpty(part))),
        _ => string.Empty,
    };

    /// <summary>An ingredient written as a line of text, or an object carrying one (Mealie's <c>display</c> or <c>note</c>).</summary>
    private static string? IngredientLine(JsonElement item) => item.ValueKind switch
    {
        JsonValueKind.String => item.GetString(),
        JsonValueKind.Object => IngredientTextFields.Select(field => Text(Property(item, field))).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)),
        _ => null,
    };

    /// <summary>Tags and categories: strings, comma-separated strings, or objects with a name.</summary>
    private static IEnumerable<string> Labels(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => (value.GetString() ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        JsonValueKind.Array => value.EnumerateArray().SelectMany(Labels),
        JsonValueKind.Object when Text(Property(value, "name")) is { } named => [named],
        _ => [],
    };

    private static IEnumerable<string> Strings(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => [value.GetString() ?? string.Empty],
        JsonValueKind.Array => value.EnumerateArray().SelectMany(Strings),
        _ => [],
    };
}
