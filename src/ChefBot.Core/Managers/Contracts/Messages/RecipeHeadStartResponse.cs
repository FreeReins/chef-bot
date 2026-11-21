namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Something a recipe step needs started or allowed for before reaching it.</summary>
/// <param name="Kind">What sort of head start it is.</param>
/// <param name="Text">The sentence of the step that asks for it: "Preheat the oven to 400°F".</param>
public sealed record RecipeHeadStartResponse(HeadStartKind Kind, string Text);
