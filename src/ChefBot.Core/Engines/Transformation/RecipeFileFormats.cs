using ChefBot.Core.Contracts;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines.Transformation;

/// <summary>The recipes read from an uploaded file, and how many entries in it couldn't be used.</summary>
internal sealed record RecipeFile(IReadOnlyList<RecipeText> Recipes, int Skipped);

/// <summary>
/// One kind of recipe file ChefBot can import. To support another kind, implement this and register it in
/// <c>CoreRegistration</c>; <see cref="TransformationEngine"/> and the web page pick it up from there. Register
/// specific formats before general ones that share a file name ending.
/// </summary>
internal interface IRecipeFileFormat
{
    /// <summary>What people call this kind of file, shown on the web page.</summary>
    string Name { get; }

    /// <summary>The file name endings it reads, lower case with the dot, such as ".json".</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// Whether this format recognises the text. Several formats can share a file name ending (Mealie backups and
    /// recipe JSON are both .json); the reader uses the first registered one that recognises the file.
    /// </summary>
    bool CanRead(string text);

    /// <summary>Reads the file's text. Entries that aren't usable recipes are counted as skipped.</summary>
    /// <exception cref="RuleViolationException">The text can't be read as this kind of file.</exception>
    RecipeFile Read(string text);
}
