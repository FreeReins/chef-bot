using System.Text;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Writes recipes as plain UTF-8 text, in the same layout the web page's "Share recipe" gives (<c>recipeText</c> in
/// <c>src/ChefBot.Web/src/lib/exporting.js</c>), so a shared recipe and a downloaded one read the same. Several
/// recipes are separated by a line of dashes. It is a helper of <see cref="TransformationEngine"/>, not an engine.
/// </summary>
internal static class RecipeTextWriter
{
    private static readonly string Rule = new('-', 40);

    /// <summary>The recipes, in the order given, as one text file.</summary>
    public static byte[] Write(IEnumerable<Recipe> recipes) =>
        Encoding.UTF8.GetBytes(string.Join($"\n{Rule}\n\n", recipes.Select(recipe => Text(RecipeDocument.From(recipe)))));

    /// <summary>One recipe as text, ending with a line break.</summary>
    public static string Text(RecipeDocument recipe)
    {
        var parts = new List<string> { $"{recipe.Title}\n{recipe.Facts}" };
        if (recipe.Description is { } description)
        {
            parts.Add(description);
        }

        parts.Add(string.Join('\n', recipe.Ingredients.Select(line => $"- {line}").Prepend("Ingredients")));
        if (recipe.Steps.Count > 0)
        {
            parts.Add(string.Join('\n', recipe.Steps.Select((step, index) => $"{index + 1}. {step}").Prepend("Steps")));
        }

        if (recipe.Notes is { } notes)
        {
            parts.Add($"Notes\n{notes}");
        }

        var credits = new[]
        {
            recipe.YieldText is { } yieldText ? $"Makes {yieldText}" : null,
            recipe.Author is { } author ? $"By {author}" : null,
            recipe.SourceUrl is { } source ? $"Source: {source}" : null,
        }.OfType<string>().ToList();
        if (credits.Count > 0)
        {
            parts.Add(string.Join('\n', credits));
        }

        return $"{string.Join("\n\n", parts)}\n";
    }
}
