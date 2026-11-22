using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies CookbookEntity to and from Cookbook.</content>
internal static partial class EntityMapping
{
    public static Cookbook ToContract(this CookbookEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        UpdatedAt = entity.UpdatedAt,
    };

    public static CookbookEntity ToEntity(this Cookbook cookbook) => new()
    {
        Id = cookbook.Id,
        Name = cookbook.Name,
        Description = cookbook.Description,
        UpdatedAt = cookbook.UpdatedAt,
    };
}
