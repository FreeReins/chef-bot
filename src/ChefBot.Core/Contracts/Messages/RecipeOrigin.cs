using System.Text.Json.Serialization;

namespace ChefBot.Core.Contracts.Messages;

/// <summary>Where a recipe came from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeOrigin>))]
public enum RecipeOrigin
{
    /// <summary>Entered or edited in ChefBot. Imports never overwrite it.</summary>
    Local,

    /// <summary>Imported from Mealie and refreshed by the next import.</summary>
    Mealie,
}
