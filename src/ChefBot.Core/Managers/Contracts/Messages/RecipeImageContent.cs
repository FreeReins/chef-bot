namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A recipe photo's bytes, for sending to the browser as an image file.</summary>
/// <param name="ContentType">Its media type, such as image/jpeg.</param>
/// <param name="Data">The image file.</param>
public sealed record RecipeImageContent(string ContentType, byte[] Data);
