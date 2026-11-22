namespace ChefBot.Core.Tests;

/// <summary>
/// Keeps source files small enough to read whole. A change to one area shouldn't mean reading every other area
/// of the same class, so a file over <see cref="Limit"/> lines is split: a C# type into partial
/// <c>&lt;Type&gt;.&lt;Area&gt;.cs</c> files, a web module into <c>src/lib/&lt;area&gt;.svelte.js</c>.
/// <see cref="Allowed"/> lists the files still over the limit. It can only shrink: a listed file can't grow,
/// and its entry has to go once the file is back under the limit.
/// </summary>
public class FileSizeTests
{
    private const int Limit = 400;

    /// <summary>Files allowed over the limit, each capped at its length when it was listed.</summary>
    private static readonly Dictionary<string, int> Allowed = new()
    {
        // One algorithm: choosing the week's meals reads best in one place.
        ["src/ChefBot.Core/Engines/PlanningEngine.cs"] = 528,

        // Page layout for one document; not yet split.
        ["src/ChefBot.Core/Engines/Transformation/RecipePdfWriter.cs"] = 426,

        // Screens with their markup and styles; their logic could move to src/lib/.
        ["src/ChefBot.Web/src/tabs/RecipeCook.svelte"] = 499,
        ["src/ChefBot.Web/src/tabs/Recipes.svelte"] = 422,
        ["src/ChefBot.Web/src/tabs/RecipeDetail.svelte"] = 425,
    };

    private static readonly string Root = FindRoot();

    [Test]
    public async Task Source_files_stay_under_the_limit()
    {
        var tooLong = Sources()
            .Select(path => (Path: path, Lines: Lines(path)))
            .Where(file => file.Lines > Allowed.GetValueOrDefault(file.Path, Limit))
            .Select(file => Allowed.TryGetValue(file.Path, out var cap)
                ? $"{file.Path} has {file.Lines} lines and may not grow past {cap}"
                : $"{file.Path} has {file.Lines} lines, over the {Limit} limit")
            .ToList();

        await Assert.That(tooLong).IsEmpty()
            .Because("long files cost every reader. Split by area into <Type>.<Area>.cs partial files, or "
                + "src/lib/<area>.svelte.js modules (CONTRIBUTING.md, \"Where things go\")");
    }

    [Test]
    public async Task Allowed_files_are_still_over_the_limit()
    {
        var stale = Allowed.Keys
            .Where(path => !File.Exists(Path.Combine(Root, path)) || Lines(path) <= Limit)
            .ToList();

        await Assert.That(stale).IsEmpty().Because("these files are now under the limit: remove them from Allowed");
    }

    private static IEnumerable<string> Sources()
    {
        var web = Path.Combine(Root, "src", "ChefBot.Web", "src");
        return Directory.EnumerateFiles(Path.Combine(Root, "src"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(web, "*.svelte", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(web, "*.js", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(Root, path).Replace('\\', '/'))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal) && !path.Contains("/obj/", StringComparison.Ordinal));
    }

    private static int Lines(string path) => File.ReadAllLines(Path.Combine(Root, path)).Length;

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ChefBot.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("ChefBot.slnx wasn't found above the test folder.");
    }
}
