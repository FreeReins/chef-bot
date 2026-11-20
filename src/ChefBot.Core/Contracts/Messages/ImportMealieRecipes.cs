namespace ChefBot.Core.Contracts.Messages;

/// <summary>Command: copy recipes from Mealie into the local database. Runs in the background.</summary>
/// <param name="StageWeekAfter">Set by the weekly schedule: the week to stage once the import is over.</param>
public sealed record ImportMealieRecipes(DateOnly? StageWeekAfter = null);
