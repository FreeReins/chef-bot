using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

/// <summary>Where an ingredient the plan needs will come from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<IngredientStatus>))]
public enum IngredientStatus
{
    ToBuy,
    Purchased,
    InPantry,
    Staple,
}
