using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies ShoppingTargetEntity to and from ShoppingTarget.</content>
internal static partial class EntityMapping
{
    public static ShoppingTarget ToContract(this ShoppingTargetEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Kind = entity.Kind,
        Url = entity.Url,
        Token = entity.Token,
        List = entity.List,
        Position = entity.Position,
    };

    public static ShoppingTargetEntity ToEntity(this ShoppingTarget target) => new()
    {
        Id = target.Id,
        Name = target.Name,
        Kind = target.Kind,
        Url = target.Url,
        Token = target.Token,
        List = target.List,
        Position = target.Position,
    };
}
