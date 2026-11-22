using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Where suggestions for a recipe stand.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeSuggestionStatus>))]
public enum RecipeSuggestionStatus
{
    /// <summary>Waiting for someone to look at them.</summary>
    Waiting,

    /// <summary>Some or all were applied to the recipe.</summary>
    Applied,

    /// <summary>Someone looked and kept the recipe as it was.</summary>
    Dismissed,

    /// <summary>The model had nothing to add.</summary>
    NothingToAdd,
}
