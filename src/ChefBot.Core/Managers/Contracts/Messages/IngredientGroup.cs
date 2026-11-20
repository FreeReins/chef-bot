using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The role an ingredient plays. Creative meals combine a whole grain, a protein and produce.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<IngredientGroup>))]
public enum IngredientGroup
{
    Other,
    WholeGrain,
    Protein,
    Vegetable,
    Fruit,
    Herb,
    Dairy,
    Staple,
}
