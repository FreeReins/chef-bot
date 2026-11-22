namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>An image sent to an AI model.</summary>
/// <param name="Data">The image file's bytes.</param>
/// <param name="MediaType">Its media type, such as image/jpeg.</param>
internal sealed record AiImage(byte[] Data, string MediaType);
