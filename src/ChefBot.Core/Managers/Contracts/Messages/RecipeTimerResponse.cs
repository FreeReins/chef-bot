namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A time given in a recipe step, as a timer to start.</summary>
/// <param name="Text">The time as written: "20 to 25 minutes".</param>
/// <param name="Seconds">How long the timer runs: the shorter end of a range, so the food is checked in time.</param>
public sealed record RecipeTimerResponse(string Text, int Seconds);
