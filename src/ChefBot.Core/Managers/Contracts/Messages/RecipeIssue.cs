using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Something a recipe is missing that makes it harder to choose, plan, shop for or cook.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeIssue>))]
public enum RecipeIssue
{
    /// <summary>It has no photo.</summary>
    NoPhoto,

    /// <summary>It has no description.</summary>
    NoDescription,

    /// <summary>It gives no preparation, cooking or total time.</summary>
    NoTimes,

    /// <summary>It has no tags.</summary>
    NoTags,

    /// <summary>Its steps are missing or too short to cook from.</summary>
    ThinSteps,

    /// <summary>Some ingredient lines have no amount, so they can't be added up for shopping.</summary>
    MissingAmounts,
}
