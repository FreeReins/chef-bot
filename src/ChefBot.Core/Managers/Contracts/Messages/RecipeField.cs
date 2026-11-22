using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>What part of a recipe a suggested change is to.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RecipeField>))]
public enum RecipeField
{
    /// <summary>A short summary, for a recipe that has none.</summary>
    Description,

    /// <summary>What it makes in words, such as "24 cookies", for a recipe that doesn't say.</summary>
    YieldText,

    /// <summary>Preparation minutes, for a recipe that doesn't say.</summary>
    PrepMinutes,

    /// <summary>Cooking minutes, for a recipe that doesn't say.</summary>
    CookMinutes,

    /// <summary>The whole time in minutes, for a recipe that doesn't say.</summary>
    TotalMinutes,

    /// <summary>Tags, for a recipe that has none.</summary>
    Tags,

    /// <summary>Clearer steps in place of short or missing ones.</summary>
    Instructions,

    /// <summary>One ingredient line, with the amount it was missing.</summary>
    IngredientAmount,

    /// <summary>An ingredient the steps use that the list leaves out, added at its end.</summary>
    NewIngredient,

    /// <summary>Tips added to the end of the notes.</summary>
    Notes,
}
