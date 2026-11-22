namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The household's kitchen achievements: first steps, badges, points and the chef rank they add up to.</summary>
/// <param name="Points">Points earned from first steps, badge tiers and dinners cooked.</param>
/// <param name="Rank">The chef rank reached, from 0 (Dishwasher, as the page names it) up to 5.</param>
/// <param name="RankPoints">The points <paramref name="Rank"/> starts at.</param>
/// <param name="NextRankPoints">The points the next rank starts at; null at the top rank.</param>
/// <param name="Starter">The first steps, in the order to take them; optional ones last.</param>
/// <param name="Badges">The badges, each with bronze, silver and gold tiers.</param>
/// <param name="Unseen">
/// New tiers and ranks the signed-in person hasn't been shown yet, oldest first. Show them, then say so with
/// <c>POST /api/achievements/seen</c>.
/// </param>
public sealed record AchievementsResponse(
    int Points,
    int Rank,
    int RankPoints,
    int? NextRankPoints,
    IReadOnlyList<Achievement> Starter,
    IReadOnlyList<Achievement> Badges,
    IReadOnlyList<AchievementUnlockResponse> Unseen);
