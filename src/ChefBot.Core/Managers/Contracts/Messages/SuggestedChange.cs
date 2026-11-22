namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One change an AI model suggests for a recipe, to take or leave.</summary>
/// <param name="Id">The change's number within the suggestion, used to apply it.</param>
/// <param name="Field">What part of the recipe it changes.</param>
/// <param name="Current">What the recipe has now: empty when nothing, one entry for a single value, the lines or steps for a list.</param>
/// <param name="Suggested">What it would become: one entry for a single value, the lines or steps for a list. For notes, the text added at the end.</param>
/// <param name="Line">For an ingredient amount, the line's position in the list, from 0.</param>
public sealed record SuggestedChange(int Id, RecipeField Field, IReadOnlyList<string> Current, IReadOnlyList<string> Suggested, int? Line = null);
