namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How far the household has come with one achievement. The web page chooses the names and wording.</summary>
/// <param name="Kind">Which achievement.</param>
/// <param name="Tier">The tier reached: 0 when not yet, up to <paramref name="Tiers"/>.</param>
/// <param name="Tiers">How many tiers it has: 1 for a first step, 3 (bronze, silver, gold) for a badge.</param>
/// <param name="Progress">What it counts so far, such as dinners cooked.</param>
/// <param name="Goal">What <paramref name="Progress"/> has to reach for the next tier; null once the last is reached.</param>
/// <param name="Optional">True for a step that is only a suggestion: it earns no points and isn't needed to finish the first steps.</param>
public sealed record Achievement(AchievementKind Kind, int Tier, int Tiers, int Progress, int? Goal, bool Optional);
