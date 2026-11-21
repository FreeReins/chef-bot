using System.Text;
using System.Text.RegularExpressions;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class RecipeDocumentExportTests
{
    private static readonly RecipeDraft Tacos = new(
        "Slaw Fish Tacos",
        RecipeCourse.Main,
        ["600 g fish fillets", "1/2 head cabbage", "3 limes"],
        Servings: 4,
        Description: "Crisp fish and a sharp lime slaw.",
        Instructions: ["Shred the cabbage and dress it with lime.", "Roast the fish for 8 to 10 minutes."],
        PrepMinutes: 20,
        CookMinutes: 10,
        TotalMinutes: 90,
        SourceUrl: "https://example.com/slaw-fish-tacos",
        Author: "Jo Park",
        YieldText: "8 tacos",
        Notes: "Any firm white fish works.",
        Tags: ["quick", "fish"]);

    private static async Task<RecipeExportFile> Export(RecipeDraft draft, RecipeExportFormat format)
    {
        var recipes = new TestCore().RecipeManager();
        var saved = await recipes.CreateAsync(draft, default);
        return await recipes.ExportAsync(saved.Id, format, default);
    }

    private static string Text(RecipeExportFile file) => Encoding.UTF8.GetString(file.Data);

    private static string Latin1(RecipeExportFile file) => Encoding.Latin1.GetString(file.Data);

    [Test]
    public async Task A_recipe_downloads_as_the_text_sharing_gives()
    {
        var file = await Export(Tacos, RecipeExportFormat.Text);

        await Assert.That(file.FileName).IsEqualTo("slaw-fish-tacos.txt");
        await Assert.That(file.ContentType).IsEqualTo("text/plain; charset=utf-8");
        await Assert.That(Text(file)).IsEqualTo(
            """
            Slaw Fish Tacos
            Serves 4 · Prep 20 min · Cook 10 min · Total 1 h 30 min

            Crisp fish and a sharp lime slaw.

            Ingredients
            - 600 g fish fillets
            - 1/2 head cabbage
            - 3 limes

            Steps
            1. Shred the cabbage and dress it with lime.
            2. Roast the fish for 8 to 10 minutes.

            Notes
            Any firm white fish works.

            Makes 8 tacos
            By Jo Park
            Source: https://example.com/slaw-fish-tacos

            """.ReplaceLineEndings("\n"));
    }

    [Test]
    public async Task A_recipe_downloads_as_markdown()
    {
        var file = await Export(Tacos, RecipeExportFormat.Markdown);
        var markdown = Text(file);

        await Assert.That(file.FileName).IsEqualTo("slaw-fish-tacos.md");
        await Assert.That(file.ContentType).IsEqualTo("text/markdown; charset=utf-8");
        await Assert.That(markdown).StartsWith("# Slaw Fish Tacos\n\n*Serves 4 · Prep 20 min · Cook 10 min · Total 1 h 30 min*\n");
        await Assert.That(markdown).Contains("## Ingredients\n\n- 600 g fish fillets\n- 1/2 head cabbage\n- 3 limes\n");
        await Assert.That(markdown).Contains("## Steps\n\n1. Shred the cabbage and dress it with lime.\n2. Roast");
        await Assert.That(markdown).Contains("## Notes\n\nAny firm white fish works.");
        await Assert.That(markdown).Contains("[Source](<https://example.com/slaw-fish-tacos>)");
        await Assert.That(markdown).Contains("Tags: quick, fish");
    }

    [Test]
    [Arguments("Tacos *extra* #1", @"Tacos \*extra\* \#1")]
    [Arguments("- not a list", @"\- not a list")]
    [Arguments("1. not a list", @"1\. not a list")]
    [Arguments("1.5 cups flour", "1.5 cups flour")]
    [Arguments("[link](x)", @"\[link\](x)")]
    public async Task Markdown_shows_the_recipes_own_words_as_written(string text, string expected)
    {
        await Assert.That(RecipeMarkdownWriter.Escape(text)).IsEqualTo(expected);
    }

    [Test]
    public async Task A_recipe_downloads_as_a_pdf()
    {
        var file = await Export(Tacos with { Ingredients = ["½ cup crème fraîche", "1 (14 oz) can beans"] }, RecipeExportFormat.Pdf);
        var pdf = Latin1(file);

        await Assert.That(file.FileName).IsEqualTo("slaw-fish-tacos.pdf");
        await Assert.That(file.ContentType).IsEqualTo("application/pdf");
        await Assert.That(pdf).StartsWith("%PDF-1.4\n");
        await Assert.That(pdf.TrimEnd()).EndsWith("%%EOF");
        await Assert.That(pdf).Contains("(Slaw Fish Tacos) Tj");
        await Assert.That(pdf).Contains("/Count 1");
        await Assert.That(pdf).Contains(@"(\275 cup cr\350me fra\356che) Tj"); // ½, è and î in Windows-1252
        await Assert.That(pdf).Contains(@"(1 \(14 oz\) can beans) Tj");
        await Assert.That(pdf).Contains("(Page 1 of 1) Tj");
    }

    [Test]
    public async Task The_pdf_table_points_at_each_object()
    {
        var pdf = Latin1(await Export(Tacos, RecipeExportFormat.Pdf));

        var start = long.Parse(Regex.Match(pdf, @"startxref\n(\d+)\n").Groups[1].Value);
        await Assert.That(pdf[(int)start..]).StartsWith("xref\n");
        var offsets = Regex.Matches(pdf, @"^(\d{10}) 00000 n $", RegexOptions.Multiline).Select(m => int.Parse(m.Groups[1].Value)).ToList();
        await Assert.That(offsets.Count).IsGreaterThan(5);
        for (var index = 0; index < offsets.Count; index++)
        {
            await Assert.That(pdf[offsets[index]..]).StartsWith($"{index + 1} 0 obj\n");
        }
    }

    [Test]
    public async Task A_long_recipe_runs_onto_more_pages()
    {
        var steps = Enumerable.Range(1, 80).Select(step => $"Step number {step}, stirring all the while.").ToList();
        var pdf = Latin1(await Export(Tacos with { Instructions = steps }, RecipeExportFormat.Pdf));

        var count = int.Parse(Regex.Match(pdf, @"/Count (\d+)").Groups[1].Value);
        await Assert.That(count).IsGreaterThan(1);
        await Assert.That(pdf).Contains($"(Page {count} of {count}) Tj");
    }

    [Test]
    public async Task Characters_the_pdf_fonts_lack_are_spelled_out_or_marked()
    {
        await Assert.That(Encoding.Latin1.GetString(RecipePdfWriter.Encode("⅓ cup — 寿司"))).IsEqualTo("1/3 cup \u0097 ??");
    }

    [Test]
    public async Task Wrapped_lines_fit_the_width()
    {
        var text = string.Join(' ', Enumerable.Repeat("Simmer gently until the sauce thickens", 10));

        var lines = RecipePdfWriter.Wrap(text, RecipePdfWriter.Font.Regular, 11, 200);

        await Assert.That(lines.Count).IsGreaterThan(1);
        foreach (var line in lines)
        {
            await Assert.That(RecipePdfWriter.Measure(line, RecipePdfWriter.Font.Regular, 11)).IsLessThanOrEqualTo(200);
        }

        await Assert.That(string.Join(' ', lines)).IsEqualTo(text);
    }

    [Test]
    public async Task A_word_too_long_for_the_line_is_split()
    {
        var word = new string('m', 60);

        var lines = RecipePdfWriter.Wrap(word, RecipePdfWriter.Font.Bold, 11, 100);

        await Assert.That(lines.Count).IsGreaterThan(1);
        await Assert.That(string.Concat(lines)).IsEqualTo(word);
    }

    [Test]
    [Arguments(RecipeExportFormat.Pdf, "pdf")]
    [Arguments(RecipeExportFormat.Markdown, "md")]
    [Arguments(RecipeExportFormat.Text, "txt")]
    public async Task The_whole_library_downloads_in_name_order(RecipeExportFormat format, string extension)
    {
        var recipes = new TestCore().RecipeManager();
        await recipes.CreateAsync(Tacos, default);
        await recipes.CreateAsync(new RecipeDraft("Overnight Oats", RecipeCourse.Breakfast, ["1 cup oats", "1 cup milk"]), default);
        var archived = await recipes.CreateAsync(new RecipeDraft("Old Stew", RecipeCourse.Main, ["1 lb beef"]), default);
        await recipes.ArchiveAsync(archived.Id, default);

        var file = await recipes.ExportAllAsync(format, default);
        var text = Latin1(file);

        await Assert.That(file.FileName).Matches($@"^chefbot-recipes-\d{{4}}-\d\d-\d\d\.{extension}$");
        await Assert.That(text.IndexOf("Overnight Oats", StringComparison.Ordinal)).IsGreaterThanOrEqualTo(0);
        await Assert.That(text.IndexOf("Overnight Oats", StringComparison.Ordinal))
            .IsLessThan(text.IndexOf("Slaw Fish Tacos", StringComparison.Ordinal));
        await Assert.That(text).DoesNotContain("Old Stew");
        if (format == RecipeExportFormat.Pdf)
        {
            await Assert.That(text).Contains("/Count 2"); // each recipe starts a page
        }
    }
}
