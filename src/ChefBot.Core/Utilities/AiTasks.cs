using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Utilities;

/// <summary>What each <see cref="AiTask"/> is called on the page, and what its model needs to be able to do.</summary>
internal static class AiTasks
{
    public static string Label(AiTask task) => task switch
    {
        AiTask.RecipeFromPhoto => "Reading recipe photos",
        AiTask.RecipeDetails => "Completing recipes",
        _ => task.ToString(),
    };

    public static string Description(AiTask task) => task switch
    {
        AiTask.RecipeFromPhoto => "Reads a recipe from photos of a cookbook page, a card or a screenshot. Needs a model that can see images.",
        AiTask.RecipeDetails => "Suggests what a recipe is missing: details, clearer steps, ingredient amounts and tips.",
        _ => string.Empty,
    };
}
