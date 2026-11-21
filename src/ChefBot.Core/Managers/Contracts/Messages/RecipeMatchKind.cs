using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What a plan style's rule looks for in a recipe.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeMatchKind>))]
public enum RecipeMatchKind
{
    /// <summary>The recipe is made in a slow cooker.</summary>
    SlowCooker,

    /// <summary>The recipe has any of the rule's tags.</summary>
    Tag,

    /// <summary>The recipe uses any of the rule's ingredients.</summary>
    Ingredient,

    /// <summary>The recipe is ready within the rule's number of minutes.</summary>
    ReadyWithin,
}
