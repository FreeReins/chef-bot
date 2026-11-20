namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A photo of a recipe. The bytes are stored on their own, as a <see cref="RecipeImageFile"/>.</summary>
internal sealed class RecipeImage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The photo's place among the recipe's photos. The first is the cover.</summary>
    public int Position { get; set; }

    /// <summary>Where an imported photo was downloaded from. Null for a photo someone uploaded.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>The image's media type, such as image/jpeg. Null until its bytes have been downloaded.</summary>
    public string? ContentType { get; set; }

    public bool IsStored => ContentType is not null;
}
