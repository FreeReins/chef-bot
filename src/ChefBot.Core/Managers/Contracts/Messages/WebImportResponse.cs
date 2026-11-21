namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe added from a web page. Its photos are downloaded in the background afterwards.</summary>
/// <param name="Recipe">The recipe as saved.</param>
/// <param name="ReadWith">The name of the reader that read the page, such as "Any site (schema.org)".</param>
public sealed record WebImportResponse(RecipeResponse Recipe, string ReadWith);
