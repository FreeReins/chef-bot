using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What part of the week's eating a recipe is for.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeCourse>))]
public enum RecipeCourse
{
    /// <summary>A dinner, which may also feed the next days' lunches.</summary>
    Main,
    Breakfast,
    Snack,
    Sweet,
    Drink,

    /// <summary>Sides, sauces and anything else that is never planned on its own.</summary>
    Other,
}
