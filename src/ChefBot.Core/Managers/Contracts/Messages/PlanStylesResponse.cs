namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The household's plan styles, the starter styles that can be added, and what rules can refer to.</summary>
/// <param name="Styles">The household's styles in their saved order. Empty until some are saved: planning then uses the first starter style.</param>
/// <param name="Known">Starter styles that can be added at any time. Adding one copies it, so it can be changed.</param>
/// <param name="Tags">The tags used by the household's recipes, for tag rules.</param>
/// <param name="Ingredients">The catalog's ingredient names, for ingredient rules.</param>
public sealed record PlanStylesResponse(
    IReadOnlyList<PlanStyleDetails> Styles,
    IReadOnlyList<PlanStyleDetails> Known,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Ingredients);
