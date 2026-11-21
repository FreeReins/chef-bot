namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Recipes written out as a file to download: schema.org JSON-LD, which other recipe apps (and ChefBot's own import)
/// read.
/// </summary>
/// <param name="FileName">A name to save it as, such as <c>slaw-fish-tacos.json</c>.</param>
/// <param name="ContentType">The file's media type.</param>
/// <param name="Data">The file's bytes, UTF-8 text.</param>
public sealed record RecipeExportFile(string FileName, string ContentType, byte[] Data);
