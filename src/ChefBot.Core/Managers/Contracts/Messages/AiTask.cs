using System.Text.Json.Serialization;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A job ChefBot gives an AI model. Each one uses the default model unless another is chosen for it.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AiTask>))]
public enum AiTask
{
    /// <summary>Reading a recipe from photos of a cookbook page, a recipe card or a screenshot. Needs a model that can see images.</summary>
    RecipeFromPhoto,

    /// <summary>Suggesting what a recipe is missing: details, clearer steps, ingredient amounts and tips.</summary>
    RecipeDetails,
}
