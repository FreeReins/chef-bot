namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: a Mealie import finished, successfully or not.</summary>
/// <param name="Succeeded">False when Mealie isn't set up or couldn't be read.</param>
/// <param name="Result">What changed, when it succeeded.</param>
/// <param name="Problem">What went wrong, when it failed.</param>
/// <param name="StageWeekAfter">Carried over from <see cref="ImportMealieRecipes"/>.</param>
public sealed record RecipeImportFinished(bool Succeeded, ImportResult? Result, string? Problem, DateOnly? StageWeekAfter);
