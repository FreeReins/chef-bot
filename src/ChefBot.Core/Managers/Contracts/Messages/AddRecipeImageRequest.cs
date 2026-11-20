namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A photo to add to a recipe.</summary>
/// <param name="Data">The image file's bytes, base64 in JSON: a JPEG, PNG, WebP or GIF of up to 5 MB.</param>
public sealed record AddRecipeImageRequest(byte[]? Data);
