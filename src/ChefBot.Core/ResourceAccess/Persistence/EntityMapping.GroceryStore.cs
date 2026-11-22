using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies GroceryStoreEntity to and from GroceryStore.</content>
internal static partial class EntityMapping
{
    public static GroceryStore ToContract(this GroceryStoreEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        IsMain = entity.IsMain,
        Position = entity.Position,
        Items = [.. entity.Items],
        SeasonalProduce = entity.SeasonalProduce,
        Months = [.. entity.Months],
    };

    public static GroceryStoreEntity ToEntity(this GroceryStore store) => new()
    {
        Id = store.Id,
        Name = store.Name,
        IsMain = store.IsMain,
        Position = store.Position,
        Items = [.. store.Items],
        SeasonalProduce = store.SeasonalProduce,
        Months = [.. store.Months],
    };
}
