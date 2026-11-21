using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Which part of the day or week a planned meal fills.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MealSlot>))]
public enum MealSlot
{
    Breakfast,
    Lunch,
    Dinner,
    Snack,
    Sweet,

    /// <summary>A drink made once for the week, like snacks and sweets.</summary>
    Drink,
}
