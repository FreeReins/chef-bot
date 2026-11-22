using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Services;

/// <summary>
/// A recipe file format that can also read an archive, such as a backup .zip holding the file and its photos. The
/// archive's ending is listed in <see cref="IRecipeFileFormat.Extensions"/> too, so the web page offers it.
/// </summary>
internal interface IRecipeArchiveFormat : IRecipeFileFormat
{
    /// <summary>The archive's file name ending, lower case with the dot, such as ".zip".</summary>
    string ArchiveExtension { get; }

    /// <summary>Reads the archive's recipes and the photos that go with them.</summary>
    /// <param name="archive">The archive's bytes. It must allow seeking.</param>
    /// <param name="words">The words that sort recipes into courses.</param>
    /// <exception cref="RuleViolationException">The archive can't be read as this kind of file.</exception>
    RecipeFile ReadArchive(Stream archive, ClassifierWords words);
}
