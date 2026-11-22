using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using static ChefBot.Core.Engines.Transformation.JsonValues;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Reads recipes written as JSON objects: ChefBot's own shape (<c>name</c>, <c>course</c>, <c>ingredients</c>,
/// <c>servings</c>, <c>isSlowCooker</c>, <c>description</c>, <c>instructions</c>, <c>prepTime</c>, <c>cookTime</c>,
/// <c>totalTime</c>, <c>sourceUrl</c>, <c>author</c>, <c>yield</c>, <c>notes</c>, <c>tags</c>, <c>images</c>) and
/// schema.org recipes as recipe sites and Mealie's recipe export give them (<c>recipeIngredient</c>,
/// <c>recipeInstructions</c>, <c>recipeYield</c>, <c>recipeCategory</c>, <c>keywords</c>, <c>image</c>, <c>url</c>,
/// ISO 8601 times such as <c>PT30M</c>). Times may also be minutes or words.
/// <see cref="JsonRecipeFormat"/> uses it for files, and <see cref="SchemaOrgPage"/> and the site readers in
/// <c>Sites/</c> for web pages. It is a helper, not an engine.
/// </summary>
internal sealed partial class SchemaRecipe
{
    private static readonly string[] IngredientTextFields = ["display", "originalText", "note", "text", "name"];
    private static readonly string[] LabelFields = ["tags", "recipeCategory", "categories", "keywords", "tools"];

    /// <summary>
    /// The objects in a JSON document that look like recipes: one recipe, a list of them, <c>{ "recipes": [...] }</c>,
    /// a JSON-LD <c>@graph</c>, or a page whose <c>mainEntity</c> is a recipe.
    /// </summary>
    public static IEnumerable<JsonElement> Entries(JsonElement root) => root.ValueKind switch
    {
        JsonValueKind.Array => root.EnumerateArray().SelectMany(Entries),
        JsonValueKind.Object when Property(root, "recipes") is { ValueKind: JsonValueKind.Array } list => Entries(list),
        JsonValueKind.Object when Property(root, "@graph") is { ValueKind: JsonValueKind.Array } graph =>
            graph.EnumerateArray().Where(IsSchemaRecipe),
        JsonValueKind.Object when Property(root, "@type") is not null && !IsSchemaRecipe(root) =>
            Property(root, "mainEntity") is { } main ? Entries(main) : [],
        JsonValueKind.Object => [root],
        _ => [],
    };

    /// <summary>Whether an object's <c>@type</c> is, or includes, <c>Recipe</c>.</summary>
    public static bool IsSchemaRecipe(JsonElement element) =>
        Property(element, "@type") is { } type && Strings(type).Any(t => t.Equals("Recipe", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The first recipe a web page describes in schema.org terms: in its JSON-LD scripts, or else in microdata
    /// (<c>itemtype="https://schema.org/Recipe"</c>). Text has HTML entities and tags taken out, as sites often
    /// leave them in. Relative photo addresses are left as they are; <see cref="TransformationEngine.ReadPage"/>
    /// makes them absolute.
    /// </summary>
    /// <returns>Null when the page has no recipe with a name and ingredients.</returns>
    public RecipeText? FromPage(IDocument page, ClassifierWords words)
    {
        var fromJsonLd = page.QuerySelectorAll("script[type='application/ld+json' i]")
            .Select(script => FirstIn(script.TextContent, words))
            .FirstOrDefault(recipe => recipe is not null);
        var found = fromJsonLd ?? Microdata.Recipes(page).Select(json => FirstIn(json, words)).FirstOrDefault(recipe => recipe is not null);
        return found is null ? null : CleanPageText(found);
    }

    /// <summary>The first usable recipe in some JSON text, or null when it isn't JSON or holds none.</summary>
    public RecipeText? FirstIn(string json, ClassifierWords words)
    {
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            return Entries(document.RootElement).Select(entry => Read(entry, words)).FirstOrDefault(recipe => recipe is not null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>One recipe object. Null when it has no name or no ingredients.</summary>
    public RecipeText? Read(JsonElement entry, ClassifierWords words)
    {
        if (entry.ValueKind != JsonValueKind.Object) return null;

        var name = Text(Property(entry, "name"))?.Trim();
        var ingredients = (Property(entry, "ingredients") ?? Property(entry, "recipeIngredient")) is { ValueKind: JsonValueKind.Array } list
            ? list.EnumerateArray().Select(IngredientLine).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line!.Trim()).ToList()
            : [];
        if (string.IsNullOrEmpty(name) || ingredients.Count == 0) return null;

        var labels = LabelFields.Select(field => Property(entry, field)).OfType<JsonElement>().SelectMany(Labels).ToList();
        var (course, isSlowCooker) = RecipeClassifier.Classify(name, labels, words);
        if (Text(Property(entry, "course")) is { } written && Enum.TryParse<RecipeCourse>(written.Trim(), ignoreCase: true, out var chosen))
        {
            course = chosen;

            // A file that names its course, as ChefBot's own export does, also gives it as recipeCategory for other
            // apps; that is the course again, not a tag.
            labels.RemoveAll(label => label.Trim().Equals(chosen.ToString(), StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Text as a web page means it to read: HTML entities such as <c>&amp;amp;</c> decoded, tags removed and runs of
    /// white space made one space. Null when nothing is left.
    /// </summary>
    public static string? PageText(string? text) =>
        text is null ? null : Tidy(Spaces().Replace(WebUtility.HtmlDecode(HtmlTag().Replace(text, " ")), " "));

    /// <summary>The recipe with every piece of text passed through <see cref="PageText"/>.</summary>
    private static RecipeText CleanPageText(RecipeText recipe)
    {
        static List<string>? Lines(IReadOnlyList<string>? lines) => lines?.Select(PageText).OfType<string>().ToList();

        return recipe with
        {
            Name = PageText(recipe.Name) ?? recipe.Name,
            Ingredients = Lines(recipe.Ingredients) ?? [],
            Description = PageText(recipe.Description),
            Instructions = Lines(recipe.Instructions),
            Author = PageText(recipe.Author),
            YieldText = PageText(recipe.YieldText),
            Notes = PageText(recipe.Notes),
            Tags = Lines(recipe.Tags),
        };
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

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();
}
