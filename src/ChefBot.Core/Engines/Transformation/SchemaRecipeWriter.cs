using System.Globalization;
using System.Text.Json;
using System.Xml;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Writes recipes as schema.org JSON-LD, the mirror of <see cref="SchemaRecipe"/>: one <c>Recipe</c> object, or a
/// <c>@graph</c> of them. Besides the schema.org fields it writes <c>course</c>, <c>isSlowCooker</c> and <c>notes</c>,
/// which schema.org has no word for; other apps ignore them and ChefBot reads them back, so nothing is lost going
/// round. Only photos that came from the web are listed, by their original address: uploaded ones have no address
/// anyone else could open. It is a helper of <see cref="TransformationEngine"/>, not an engine.
/// </summary>
internal static class SchemaRecipeWriter
{
    private const string Context = "https://schema.org";

    /// <summary>One recipe as a JSON-LD document.</summary>
    public static byte[] Write(Recipe recipe) => Document(writer => WriteRecipe(writer, recipe, withContext: true));

    /// <summary>Several recipes as one JSON-LD document whose <c>@graph</c> lists them.</summary>
    public static byte[] Write(IEnumerable<Recipe> recipes) => Document(writer =>
    {
        writer.WriteStartObject();
        writer.WriteString("@context", Context);
        writer.WriteStartArray("@graph");
        foreach (var recipe in recipes)
        {
            WriteRecipe(writer, recipe, withContext: false);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    });

    private static byte[] Document(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            write(writer);
        }

        return buffer.ToArray();
    }

    private static void WriteRecipe(Utf8JsonWriter writer, Recipe recipe, bool withContext)
    {
        writer.WriteStartObject();
        if (withContext)
        {
            writer.WriteString("@context", Context);
        }

        writer.WriteString("@type", "Recipe");
        writer.WriteString("name", recipe.Name);
        Optional(writer, "description", recipe.Description);
        if (recipe.Course != RecipeCourse.Other)
        {
            writer.WriteString("recipeCategory", recipe.Course.ToString());
        }

        if (recipe.Tags.Count > 0)
        {
            writer.WriteString("keywords", string.Join(", ", recipe.Tags));
        }

        // The servings as a number first, then the wording when it says more, as recipe sites write it.
        var servings = recipe.Servings.ToString("0.##", CultureInfo.InvariantCulture);
        writer.WriteStartArray("recipeYield");
        writer.WriteStringValue(servings);
        if (recipe.YieldText is { } yieldText)
        {
            writer.WriteStringValue(yieldText);
        }

        writer.WriteEndArray();

        writer.WriteStartArray("recipeIngredient");
        foreach (var ingredient in recipe.Ingredients.OrderBy(ingredient => ingredient.Position))
        {
            writer.WriteStringValue(ingredient.Text);
        }

        writer.WriteEndArray();

        writer.WriteStartArray("recipeInstructions");
        foreach (var step in recipe.Instructions)
        {
            writer.WriteStartObject();
            writer.WriteString("@type", "HowToStep");
            writer.WriteString("text", step);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        Duration(writer, "prepTime", recipe.PrepMinutes);
        Duration(writer, "cookTime", recipe.CookMinutes);
        Duration(writer, "totalTime", recipe.TotalMinutes);
        if (recipe.Author is { } author)
        {
            writer.WriteStartObject("author");
            writer.WriteString("@type", "Person");
            writer.WriteString("name", author);
            writer.WriteEndObject();
        }

        Optional(writer, "url", recipe.SourceUrl);
        var photos = recipe.Images.OrderBy(image => image.Position).Select(image => image.SourceUrl).OfType<string>().ToList();
        if (photos.Count > 0)
        {
            writer.WriteStartArray("image");
            photos.ForEach(writer.WriteStringValue);
            writer.WriteEndArray();
        }

        writer.WriteString("dateModified", recipe.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));

        // ChefBot's own fields, read back by SchemaRecipe.
        writer.WriteString("course", recipe.Course.ToString());
        writer.WriteBoolean("isSlowCooker", recipe.IsSlowCooker);
        Optional(writer, "notes", recipe.Notes);
        writer.WriteEndObject();
    }

    private static void Optional(Utf8JsonWriter writer, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writer.WriteString(name, value);
        }
    }

    /// <summary>A time as ISO 8601, such as <c>PT1H30M</c>.</summary>
    private static void Duration(Utf8JsonWriter writer, string name, int? minutes)
    {
        if (minutes is > 0)
        {
            writer.WriteString(name, XmlConvert.ToString(TimeSpan.FromMinutes(minutes.Value)));
        }
    }
}
