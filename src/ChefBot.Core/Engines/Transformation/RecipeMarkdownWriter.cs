using System.Text;
using System.Text.RegularExpressions;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>
/// Writes recipes as Markdown (CommonMark), for notes apps such as Obsidian: the name as a heading, the facts in
/// italics, the ingredients as a list and the steps as a numbered list, then the notes and credits with the source as
/// a link. The recipe's own words are escaped, so a "*" or "#" in them is shown as written. Several recipes are
/// separated by a horizontal rule. It is a helper of <see cref="TransformationEngine"/>, not an engine.
/// </summary>
internal static partial class RecipeMarkdownWriter
{
    /// <summary>The recipes, in the order given, as one Markdown file.</summary>
    public static byte[] Write(IEnumerable<Recipe> recipes) =>
        Encoding.UTF8.GetBytes(string.Join("\n---\n\n", recipes.Select(recipe => Markdown(RecipeDocument.From(recipe)))));

    /// <summary>One recipe as Markdown, ending with a line break.</summary>
    public static string Markdown(RecipeDocument recipe)
    {
        var parts = new List<string> { $"# {Escape(recipe.Title)}", $"*{Escape(recipe.Facts)}*" };
        if (recipe.Description is { } description)
        {
            parts.Add(Escape(description));
        }

        parts.Add("## Ingredients");
        parts.Add(string.Join('\n', recipe.Ingredients.Select(line => $"- {Escape(line)}")));
        if (recipe.Steps.Count > 0)
        {
            parts.Add("## Steps");
            parts.Add(string.Join('\n', recipe.Steps.Select((step, index) => $"{index + 1}. {Escape(step)}")));
        }

        if (recipe.Notes is { } notes)
        {
            parts.Add("## Notes");
            parts.Add(Escape(notes));
        }

        var credits = new[]
        {
            recipe.YieldText is { } yieldText ? $"Makes {Escape(yieldText)}" : null,
            recipe.Author is { } author ? $"By {Escape(author)}" : null,
            recipe.SourceUrl is { } source ? $"[Source](<{source.Replace(">", "%3E", StringComparison.Ordinal)}>)" : null,
            recipe.Tags.Count > 0 ? $"Tags: {Escape(string.Join(", ", recipe.Tags))}" : null,
        }.OfType<string>().ToList();
        if (credits.Count > 0)
        {
            // Two trailing spaces end a line without starting a new paragraph.
            parts.Add(string.Join("  \n", credits));
        }

        return $"{string.Join("\n\n", parts.Where(part => part.Length > 0))}\n";
    }

    /// <summary>
    /// Text that Markdown shows exactly as written: its punctuation is backslash-escaped, and each line that would
    /// start a heading, list or quote has that start escaped too.
    /// </summary>
    public static string Escape(string text)
    {
        var lines = text.Split('\n').Select(line =>
        {
            var escaped = Punctuation().Replace(line, @"\$0");
            escaped = MarkerStart().Replace(escaped, @"$1\$2");
            return NumberStart().Replace(escaped, @"$1\$2");
        });
        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"[\\`*_\[\]<>#|~]")]
    private static partial Regex Punctuation();

    /// <summary>A "-", "+" or "=" at a line's start, which could begin a list item, a rule or a heading underline.</summary>
    [GeneratedRegex(@"^(\s*)([-+=])")]
    private static partial Regex MarkerStart();

    /// <summary>A number and "." or ")" at a line's start, as "1. " begins a numbered list.</summary>
    [GeneratedRegex(@"^(\s*\d+)([.)])(?=\s|$)")]
    private static partial Regex NumberStart();
}
