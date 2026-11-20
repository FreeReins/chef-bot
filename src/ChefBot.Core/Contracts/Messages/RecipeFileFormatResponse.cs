namespace ChefBot.Core.Contracts.Messages;

/// <summary>A kind of recipe file that can be imported, for the file picker.</summary>
public sealed record RecipeFileFormatResponse(string Name, IReadOnlyList<string> Extensions);
