using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies RecipeFavouriteEntity to and from RecipeFavourite.</content>
internal static partial class EntityMapping
{
    public static RecipeFavourite ToContract(this RecipeFavouriteEntity entity, string userName) => new()
    {
        Id = entity.Id,
        RecipeId = entity.RecipeId,
        UserId = entity.UserId,
        CreatedAt = entity.CreatedAt,
        UserName = userName,
    };

    public static RecipeFavouriteEntity ToEntity(this RecipeFavourite favourite) => new()
    {
        Id = favourite.Id,
        RecipeId = favourite.RecipeId,
        UserId = favourite.UserId,
        CreatedAt = favourite.CreatedAt,
    };
}
