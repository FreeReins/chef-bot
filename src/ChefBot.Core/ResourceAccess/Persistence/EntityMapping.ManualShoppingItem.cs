using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies ManualShoppingItemEntity to and from ManualShoppingItem.</content>
internal static partial class EntityMapping
{
    public static ManualShoppingItem ToContract(this ManualShoppingItemEntity entity) => new()
    {
        Id = entity.Id,
        Key = entity.Key,
        CatalogName = entity.CatalogName,
        DisplayName = entity.DisplayName,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        ExtraAmounts = ReadAmounts(entity.ExtraAmounts),
        AddedOn = entity.AddedOn,
    };

    public static ManualShoppingItemEntity ToEntity(this ManualShoppingItem item) => new()
    {
        Id = item.Id,
        Key = item.Key,
        CatalogName = item.CatalogName,
        DisplayName = item.DisplayName,
        Quantity = item.Quantity,
        Unit = item.Unit,
        ExtraAmounts = WriteAmounts(item.ExtraAmounts),
        AddedOn = item.AddedOn,
    };
}
