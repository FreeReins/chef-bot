using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <content>
/// The notice for new achievements. The names match the web page's (src/ChefBot.Web/src/lib/achievements.js), so the
/// webhook and the page call each one the same thing.
/// </content>
internal sealed partial class NotificationEngine
{
    private static readonly Dictionary<AchievementKind, string> StepTitles = new()
    {
        [AchievementKind.FirstRecipe] = "Stock the cookbook",
        [AchievementKind.FirstPlan] = "Plan a week",
        [AchievementKind.PlanConfirmed] = "Commit to the menu",
        [AchievementKind.FirstShop] = "Do the big shop",
        [AchievementKind.FirstCook] = "Cook something",
        [AchievementKind.FirstFavourite] = "Pick a favourite",
        [AchievementKind.FirstGuest] = "Pull up a chair",
    };

    private static readonly Dictionary<AchievementKind, string[]> BadgeNames = new()
    {
        [AchievementKind.Cookbook] = ["Recipe Collector", "Cookbook Hoarder", "Walking Library"],
        [AchievementKind.Dinners] = ["Regular Cook", "Weeknight Hero", "Dinner Machine"],
        [AchievementKind.Repertoire] = ["Dabbler", "Range Finder", "Culinary Polymath"],
        [AchievementKind.WeeksPlanned] = ["Planner", "Calendar Whisperer", "Year of Plenty"],
        [AchievementKind.Hauls] = ["Cart Pusher", "Aisle Navigator", "Supermarket Sweeper"],
        [AchievementKind.BestStreak] = ["Warming Up", "On a Roll", "Unstoppable Stove"],
        [AchievementKind.Comebacks] = ["Nostalgic", "Revival Act", "Greatest Hits DJ"],
        [AchievementKind.Critic] = ["Armchair Critic", "Food Columnist", "Michelin Inspector"],
        [AchievementKind.Favourites] = ["Smitten", "Big Softie", "Heart of the Kitchen"],
    };

    private static readonly string[] Medals = ["bronze", "silver", "gold"];

    private static readonly string[] Ranks = ["Dishwasher", "Prep Cook", "Line Cook", "Sous Chef", "Head Chef", "Legend of the Pass"];

    /// <summary>One notice for everything reached at once, such as after a big import.</summary>
    public Notification Achievements(IReadOnlyList<AchievementUnlockResponse> unlocks)
    {
        var lines = unlocks.Select(Describe).ToList();
        var title = lines.Count == 1 ? lines[0] : $"{lines.Count} new achievements";
        var message = lines.Count == 1
            ? $"{lines[0]}. See it on the Achievements page."
            : $"{string.Join("; ", lines)}. See them on the Achievements page.";
        return new Notification(NotificationTypes.Achievement, title, message, new Dictionary<string, object?>
        {
            ["achievements"] = unlocks.Select(unlock => new Dictionary<string, object?>
            {
                ["id"] = unlock.Id,
                ["kind"] = unlock.Kind?.ToString() ?? "Rank",
                ["tier"] = unlock.Tier,
                ["name"] = Describe(unlock),
            }).ToList(),
        });
    }

    /// <summary>What one unlock is called, such as "New silver badge: Weeknight Hero".</summary>
    internal static string Describe(AchievementUnlockResponse unlock) => unlock.Kind switch
    {
        null => $"Promoted to {Ranks[Math.Clamp(unlock.Tier, 0, Ranks.Length - 1)]}",
        { } kind when StepTitles.TryGetValue(kind, out var step) => $"First step done: {step}",
        { } kind when BadgeNames.TryGetValue(kind, out var names) && unlock.Tier is >= 1 and <= 3 =>
            $"New {Medals[unlock.Tier - 1]} badge: {names[unlock.Tier - 1]}",
        { } kind => $"New achievement: {kind}",
    };
}
