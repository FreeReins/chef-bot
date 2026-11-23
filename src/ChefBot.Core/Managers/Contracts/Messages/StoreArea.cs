using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// The part of a store an ingredient is usually found in. The shopping list groups items by it, in this order: the
/// order most shoppers walk a supermarket, from fresh produce to the freezers. <c>Other</c> is for anything ChefBot
/// doesn't know.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<StoreArea>))]
public enum StoreArea
{
    Produce,
    Bakery,
    MeatAndSeafood,
    DairyAndChilled,
    GrainsAndPasta,
    CannedAndDry,
    SaucesAndOils,
    International,
    SpicesAndBaking,
    Frozen,
    Drinks,
    Other,
}
