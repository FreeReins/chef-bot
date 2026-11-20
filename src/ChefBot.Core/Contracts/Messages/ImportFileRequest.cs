namespace ChefBot.Core.Contracts.Messages;

/// <summary>A recipe file to import: its name, which decides the format, and its text as read by the browser.</summary>
public sealed record ImportFileRequest(string FileName, string Content);
