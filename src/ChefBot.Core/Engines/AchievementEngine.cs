using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines;

/// <summary>
/// Works out the household's kitchen achievements from what is already kept: the first steps, the badges with
/// their bronze, silver and gold tiers, and the points and chef rank they add up to. Nothing here depends on how
/// many people are in the household, so one person cooking for themselves can earn everything; inviting someone is
/// an optional step that earns no points. Reads and saves nothing.
/// </summary>
internal sealed class AchievementEngine
{
    /// <summary>The days a recipe has to rest before cooking it again counts as a comeback.</summary>
    public const int ComebackDays = 90;

    /// <summary>The points each rank starts at, from Dishwasher (0) up.</summary>
    public static readonly int[] RankPoints = [0, 150, 500, 1200, 2500, 5000];

    private const int StepPoints = 50;

    private const int CookPoints = 5;

    private static readonly int[] TierPoints = [100, 250, 500];

    /// <summary>The achievements earned so far.</summary>
    /// <param name="moments">Every stored food moment, in any order.</param>
    /// <param name="activeRecipes">How many recipes the library holds, archived ones left out.</param>
    /// <param name="favourites">How many favourites are marked, by everyone.</param>
    /// <param name="reviews">How many reviews have been written, by everyone.</param>
    /// <param name="plans">How many plans are stored, and how many were confirmed.</param>
    public AchievementsResponse Score(
        IReadOnlyList<FoodMoment> moments, int activeRecipes, int favourites, int reviews, PlanTally plans)
    {
        var cooks = moments.Where(moment => moment.Kind == FoodMomentKind.Cooked).ToList();
        var hauls = moments.Count(moment => moment.Kind == FoodMomentKind.Haul);
        var guests = moments.Count(moment => moment.Kind == FoodMomentKind.PersonJoined);

        var starter = new List<Achievement>
        {
            Step(AchievementKind.FirstRecipe, activeRecipes),
            Step(AchievementKind.FirstPlan, plans.Total),
            Step(AchievementKind.PlanConfirmed, plans.Confirmed),
            Step(AchievementKind.FirstShop, hauls),
            Step(AchievementKind.FirstCook, cooks.Count),
            Step(AchievementKind.FirstFavourite, favourites),
            Step(AchievementKind.FirstGuest, guests, optional: true),
        };

        var badges = new List<Achievement>
        {
            Badge(AchievementKind.Cookbook, activeRecipes, 10, 50, 150),
            Badge(AchievementKind.Dinners, cooks.Count(cook => cook.Slot == MealSlot.Dinner), 10, 50, 250),
            Badge(AchievementKind.Repertoire, cooks.Where(cook => cook.RecipeId is not null).Select(cook => cook.RecipeId).Distinct().Count(), 5, 20, 50),
            Badge(AchievementKind.WeeksPlanned, plans.Total, 4, 12, 52),
            Badge(AchievementKind.Hauls, hauls, 3, 10, 25),
            Badge(AchievementKind.BestStreak, BestStreak(cooks), 3, 8, 26),
            Badge(AchievementKind.Comebacks, Comebacks(cooks), 1, 5, 15),
            Badge(AchievementKind.Critic, reviews, 1, 10, 30),
            Badge(AchievementKind.Favourites, favourites, 5, 15, 40),
        };

        var points = starter.Where(step => !step.Optional && step.Tier > 0).Sum(_ => StepPoints)
            + badges.Sum(badge => TierPoints.Take(badge.Tier).Sum())
            + cooks.Count * CookPoints;
        var rank = Array.FindLastIndex(RankPoints, start => points >= start);
        int? next = rank + 1 < RankPoints.Length ? RankPoints[rank + 1] : null;
        return new AchievementsResponse(points, rank, RankPoints[rank], next, starter, badges, []);
    }

    /// <summary>
    /// What <paramref name="current"/> reached beyond what was kept before: every tier above the highest kept for
    /// each achievement, and every rank above the highest kept. Optional steps are left out, since they earn nothing.
    /// Nothing is taken back, so a tier that drops and comes back again is not new. With nothing kept yet, rank 0
    /// (where everyone starts) is included, so the next check knows tracking has begun.
    /// </summary>
    public static IReadOnlyList<AchievementUnlock> Unlocks(
        AchievementsResponse current, IReadOnlyList<AchievementUnlock> kept, DateTimeOffset now, Guid? causedBy, bool quiet)
    {
        var fresh = new List<AchievementUnlock>();
        foreach (var achievement in current.Starter.Concat(current.Badges).Where(achievement => !achievement.Optional))
        {
            var highest = kept.Where(unlock => unlock.Kind == achievement.Kind).Select(unlock => unlock.Tier).DefaultIfEmpty(0).Max();
            for (var tier = highest + 1; tier <= achievement.Tier; tier++)
            {
                fresh.Add(new AchievementUnlock { Kind = achievement.Kind, Tier = tier, EarnedAt = now, Quiet = quiet, CausedBy = causedBy });
            }
        }

        var highestRank = kept.Where(unlock => unlock.Kind is null).Select(unlock => unlock.Tier).DefaultIfEmpty(-1).Max();
        for (var rank = highestRank + 1; rank <= current.Rank; rank++)
        {
            fresh.Add(new AchievementUnlock { Kind = null, Tier = rank, EarnedAt = now, Quiet = quiet || rank == 0, CausedBy = causedBy });
        }

        return fresh;
    }

    /// <summary>The longest run of Sunday-to-Saturday weeks in a row with at least one cook.</summary>
    internal static int BestStreak(IEnumerable<FoodMoment> cooks)
    {
        var weeks = cooks.Select(cook => cook.OccurredOn.AddDays(-(int)cook.OccurredOn.DayOfWeek)).Distinct().Order().ToList();
        int best = 0, run = 0;
        for (var index = 0; index < weeks.Count; index++)
        {
            run = index > 0 && weeks[index].DayNumber - weeks[index - 1].DayNumber == 7 ? run + 1 : 1;
            best = Math.Max(best, run);
        }

        return best;
    }

    /// <summary>How many times a recipe was cooked again after resting at least <see cref="ComebackDays"/>.</summary>
    internal static int Comebacks(IEnumerable<FoodMoment> cooks) =>
        cooks.Where(cook => cook.RecipeId is not null)
            .GroupBy(cook => cook.RecipeId)
            .Sum(recipe =>
            {
                var days = recipe.Select(cook => cook.OccurredOn.DayNumber).Order().ToList();
                return days.Zip(days.Skip(1)).Count(pair => pair.Second - pair.First >= ComebackDays);
            });

    private static Achievement Step(AchievementKind kind, int count, bool optional = false) =>
        new(kind, count > 0 ? 1 : 0, 1, Math.Min(count, 1), count > 0 ? null : 1, optional);

    private static Achievement Badge(AchievementKind kind, int count, params int[] goals)
    {
        var tier = goals.Count(goal => count >= goal);
        return new Achievement(kind, tier, goals.Length, count, tier < goals.Length ? goals[tier] : null, false);
    }
}
