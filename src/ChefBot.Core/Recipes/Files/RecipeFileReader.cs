using ChefBot.Core.Common;

namespace ChefBot.Core.Recipes.Files;

/// <summary>Reads an uploaded recipe file with the format its name ending belongs to.</summary>
public sealed class RecipeFileReader(IEnumerable<IRecipeFileFormat> formats)
{
    /// <summary>Every kind of file that can be imported.</summary>
    public IReadOnlyList<IRecipeFileFormat> Formats { get; } = formats.ToList();

    /// <exception cref="RuleViolationException">The file is of a kind ChefBot can't read, or its contents can't be read.</exception>
    public RecipeFile Read(string fileName, string text)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        var candidates = Formats.Where(f => f.Extensions.Contains(extension)).ToList();
        var format = candidates.FirstOrDefault(f => f.CanRead(text)) ?? candidates.LastOrDefault()
            ?? throw new RuleViolationException(
                $"ChefBot can't import {(extension.Length > 0 ? $"{extension} files" : "files without a type")} yet. " +
                $"Choose one of these instead: {string.Join(", ", Formats.Select(f => $"{f.Name} ({string.Join(", ", f.Extensions)})"))}.");

        return format.Read(text);
    }
}
