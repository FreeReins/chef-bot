namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One step of a recipe, with what ChefBot read from it for cooking mode.</summary>
/// <param name="Text">The step as written.</param>
/// <param name="Timers">The times the step gives, in the order written, each one a timer to start.</param>
/// <param name="HeadStart">Something in this step to start or allow for ahead of time, such as preheating the oven, or null.</param>
/// <param name="Ingredients">The positions in the recipe's ingredient list of the ingredients the step names.</param>
public sealed record RecipeStepResponse(
    string Text,
    IReadOnlyList<RecipeTimerResponse> Timers,
    RecipeHeadStartResponse? HeadStart,
    IReadOnlyList<int> Ingredients);
