namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class RecipeFavouriteEntity
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
