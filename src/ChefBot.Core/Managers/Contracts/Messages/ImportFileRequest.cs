namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe file to import: its name, which decides the format, and its text as read by the browser.</summary>
/// <param name="FileName">The file's name.</param>
/// <param name="Content">The file's text.</param>
/// <param name="CookbookId">A cookbook to put every recipe added from the file in; null for none.</param>
public sealed record ImportFileRequest(string FileName, string Content, Guid? CookbookId = null);
