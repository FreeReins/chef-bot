namespace ChefBot.Core.ResourceAccess.Persistence;

/// <summary>A recipe photo's bytes, in a table of their own so loading recipes never reads them.</summary>
internal sealed class RecipeImageDataEntity
{
    public Guid ImageId { get; set; }

    public byte[] Data { get; set; } = [];
}
