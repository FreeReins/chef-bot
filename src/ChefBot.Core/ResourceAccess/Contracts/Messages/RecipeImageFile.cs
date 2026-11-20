namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>The bytes of one recipe photo.</summary>
internal sealed record RecipeImageFile(Guid ImageId, string ContentType, byte[] Data);
