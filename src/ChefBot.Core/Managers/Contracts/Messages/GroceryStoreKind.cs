using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What sort of store a known store is, so the list of known stores can be grouped.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GroceryStoreKind>))]
public enum GroceryStoreKind
{
    Supermarket,
    Discount,
    WarehouseClub,
    NaturalFoods,
    Specialty,
    FarmersMarket,
}
