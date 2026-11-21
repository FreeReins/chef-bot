using System.Globalization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// A recipe as the parts a reader sees, in reading order, for the files people read rather than import: plain text,
/// Markdown and PDF. Each writer lays out the same parts, so the formats say the same things. It is a helper of
/// <see cref="TransformationEngine"/>, not an engine.
/// </summary>
/// <param name="Title">The recipe's name.</param>
/// <param name="Facts">"Serves 4 · Prep 20 min · Cook 10 min · Total 1 h 30 min".</param>
/// <param name="Description">A short summary of the dish, or null.</param>
/// <param name="Ingredients">The ingredient lines as the recipe writes them, in order.</param>
/// <param name="Steps">The steps, in order.</param>
/// <param name="Notes">The cook's own notes, or null.</param>
/// <param name="YieldText">What it makes, such as "8 tacos", or null.</param>
/// <param name="Author">Who wrote it, or null.</param>
/// <param name="SourceUrl">The web page it came from, or null.</param>
/// <param name="Tags">Its tags.</param>
internal sealed record RecipeDocument(
    string Title,
    string Facts,
    string? Description,
    IReadOnlyList<string> Ingredients,
    IReadOnlyList<string> Steps,
    string? Notes,
    string? YieldText,
    string? Author,
    string? SourceUrl,
    IReadOnlyList<string> Tags)
{
    /// <summary>The parts of a stored recipe. Blank text counts as missing.</summary>
    public static RecipeDocument From(Recipe recipe)
    {
        var facts = new List<string> { $"Serves {recipe.Servings.ToString("0.##", CultureInfo.InvariantCulture)}" };
        foreach (var (label, minutes) in new[] { ("Prep", recipe.PrepMinutes), ("Cook", recipe.CookMinutes), ("Total", recipe.TotalMinutes) })
        {
            if (minutes is > 0)
            {
                facts.Add($"{label} {Duration(minutes.Value)}");
            }
        }

        return new RecipeDocument(
            recipe.Name.Trim(),
            string.Join(" · ", facts),
            Tidy(recipe.Description),
            recipe.Ingredients.OrderBy(line => line.Position)
                .Select(line => string.IsNullOrWhiteSpace(line.Text) ? line.DisplayName.Trim() : line.Text.Trim())
                .Where(line => line.Length > 0)
                .ToList(),
            recipe.Instructions.Select(step => step.Trim()).Where(step => step.Length > 0).ToList(),
            Tidy(recipe.Notes),
            Tidy(recipe.YieldText),
            Tidy(recipe.Author),
            Tidy(recipe.SourceUrl),
            recipe.Tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).ToList());
    }

    /// <summary>Minutes in words, as the web page shows them: "1 h 30 min", "2 h", "45 min".</summary>
    public static string Duration(int minutes)
    {
        var hours = minutes / 60;
        var rest = minutes % 60;
        return string.Join(' ', new[] { hours > 0 ? $"{hours} h" : null, rest > 0 ? $"{rest} min" : null }.OfType<string>());
    }

    private static string? Tidy(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
}
