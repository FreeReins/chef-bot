namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Photos of one recipe to read, such as a cookbook page or a recipe card.</summary>
/// <param name="Photos">The pages in order, each a JPEG, PNG, WebP or GIF of up to 5 MB, base64 in JSON. At most four.</param>
public sealed record RecipePhotoRequest(IReadOnlyList<byte[]>? Photos);
