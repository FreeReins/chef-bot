using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>The recipes read from an uploaded file, and how many entries in it couldn't be used.</summary>
/// <param name="Recipes">The usable recipes.</param>
/// <param name="Skipped">Entries without a name or any ingredients.</param>
/// <param name="Photos">
/// A photo file's bytes for each recipe that has one, keyed by recipe name (ignoring case). Not checked yet: they may
/// not be an image ChefBot can show. Null for files that carry no photos.
/// </param>
internal sealed record RecipeFile(
    IReadOnlyList<RecipeText> Recipes,
    int Skipped,
    IReadOnlyDictionary<string, byte[]>? Photos = null);
