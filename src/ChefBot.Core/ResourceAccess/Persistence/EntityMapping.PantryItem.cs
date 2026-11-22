using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies PantryItemEntity to and from PantryItem.</content>
internal static partial class EntityMapping
{
    public static PantryItem ToContract(this PantryItemEntity entity) => new()
    {
        Id = entity.Id,
        Key = entity.Key,
        CatalogName = entity.CatalogName,
        DisplayName = entity.DisplayName,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        PurchasedOn = entity.PurchasedOn,
        ExpiresOn = entity.ExpiresOn,
    };

    public static PantryItemEntity ToEntity(this PantryItem item) => new()
    {
        Id = item.Id,
        Key = item.Key,
        CatalogName = item.CatalogName,
        DisplayName = item.DisplayName,
        Quantity = item.Quantity,
        Unit = item.Unit,
        PurchasedOn = item.PurchasedOn,
        ExpiresOn = item.ExpiresOn,
    };
}
