namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Recipes written out as a file to download, in one of the <see cref="RecipeExportFormat"/>s: schema.org JSON-LD,
/// which other recipe apps (and ChefBot's own import) read, or a PDF, Markdown or plain text file to read and keep.
/// </summary>
/// <param name="FileName">A name to save it as, such as <c>slaw-fish-tacos.json</c> or <c>slaw-fish-tacos.pdf</c>.</param>
/// <param name="ContentType">The file's media type.</param>
/// <param name="Data">The file's bytes: UTF-8 text, or a PDF.</param>
public sealed record RecipeExportFile(string FileName, string ContentType, byte[] Data);
