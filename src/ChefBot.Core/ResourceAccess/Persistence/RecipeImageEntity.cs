namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class RecipeImageEntity
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public int Position { get; set; }

    public string? SourceUrl { get; set; }

    public string? ContentType { get; set; }
}
