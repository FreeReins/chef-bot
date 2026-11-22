using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class AchievementTests
{
    private static readonly AchievementEngine Engine = new();

    /// <summary>A Sunday, so weeks line up with the planning week.</summary>
    private static readonly DateOnly Sunday = new(2026, 3, 1);

    private static FoodMoment Cooked(Recipe recipe, DateOnly on, MealSlot slot = MealSlot.Dinner) =>
        new() { Kind = FoodMomentKind.Cooked, RecipeId = recipe.Id, Title = recipe.Name, Slot = slot, OccurredOn = on };

    private static FoodMoment Moment(FoodMomentKind kind, DateOnly on) => new() { Kind = kind, Title = "x", OccurredOn = on };

    private static AchievementsResponse Score(
        IEnumerable<FoodMoment>? moments = null, int recipes = 0, int favourites = 0, int reviews = 0, PlanTally? plans = null) =>
        Engine.Score((moments ?? []).ToList(), recipes, favourites, reviews, plans ?? new PlanTally(0, 0));

    private static Achievement Find(AchievementsResponse response, AchievementKind kind) =>
        response.Starter.Concat(response.Badges).Single(achievement => achievement.Kind == kind);

    [Test]
    public async Task A_new_kitchen_starts_as_a_dishwasher_with_nothing_earned()
    {
        var response = Score();

        await Assert.That(response.Points).IsEqualTo(0);
        await Assert.That(response.Rank).IsEqualTo(0);
        await Assert.That(response.NextRankPoints).IsEqualTo(150);
        await Assert.That(response.Starter.Concat(response.Badges).All(achievement => achievement.Tier == 0)).IsTrue();
        await Assert.That(string.Join(", ", response.Starter.Where(step => step.Optional).Select(step => step.Kind)))
            .IsEqualTo(nameof(AchievementKind.FirstGuest));
    }

    [Test]
    public async Task A_badge_reaches_the_tiers_its_count_passes_and_names_the_next_goal()
    {
        var response = Score(recipes: 50);

        var cookbook = Find(response, AchievementKind.Cookbook);
        await Assert.That(cookbook.Tier).IsEqualTo(2);
        await Assert.That(cookbook.Goal).IsEqualTo(150);
        await Assert.That(Find(Score(recipes: 150), AchievementKind.Cookbook).Goal).IsNull();
        await Assert.That(Find(response, AchievementKind.FirstRecipe).Tier).IsEqualTo(1);
    }

    [Test]
    public async Task The_best_streak_keeps_the_longest_run_of_weeks_after_a_gap()
    {
        // Three weeks in a row, a week off, then two: the best run stays three.
        int[] weeks = [0, 1, 2, 4, 5];
        var cooks = weeks.Select(week => Cooked(TestData.Stew, Sunday.AddDays(7 * week + 3)));

        await Assert.That(Find(Score(cooks), AchievementKind.BestStreak).Progress).IsEqualTo(3);
    }

    [Test]
    public async Task A_comeback_needs_a_long_rest()
    {
        var cooks = new[]
        {
            Cooked(TestData.Soup, Sunday),
            Cooked(TestData.Soup, Sunday.AddDays(89)),
            Cooked(TestData.Soup, Sunday.AddDays(89 + AchievementEngine.ComebackDays)),
        };

        await Assert.That(Find(Score(cooks), AchievementKind.Comebacks).Progress).IsEqualTo(1);
    }

    [Test]
    public async Task Points_add_up_steps_badge_tiers_and_cooks_into_a_rank()
    {
        // One recipe, cooked as ten dinners over ten weeks: two steps, Dinners bronze and BestStreak silver.
        var cooks = Enumerable.Range(0, 10).Select(week => Cooked(TestData.Stew, Sunday.AddDays(7 * week)));

        var response = Score(cooks, recipes: 1);

        await Assert.That(response.Points).IsEqualTo(2 * 50 + 100 + (100 + 250) + 10 * 5);
        await Assert.That(response.Rank).IsEqualTo(2);
        await Assert.That(response.RankPoints).IsEqualTo(500);
        await Assert.That(response.NextRankPoints).IsEqualTo(1200);
    }

    [Test]
    public async Task Inviting_someone_ticks_the_optional_step_without_earning_points()
    {
        var response = Score([Moment(FoodMomentKind.PersonJoined, Sunday)]);

        await Assert.That(Find(response, AchievementKind.FirstGuest).Tier).IsEqualTo(1);
        await Assert.That(response.Points).IsEqualTo(0);
    }

    [Test]
    public async Task Staging_and_confirming_a_plan_ticks_the_first_plan_steps()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        var cook = new UserAccount { Username = "sam", DisplayName = "Sam" };
        core.Accounts.Users.Add(cook);

        var plan = await core.PlanManager().StageAsync(new StagePlanRequest(), default);
        var staged = await core.RecipeManager().GetAchievementsAsync(cook.Id, default);
        await core.PlanManager().ConfirmAsync(plan.Id, default);
        var confirmed = await core.RecipeManager().GetAchievementsAsync(cook.Id, default);

        await Assert.That(Find(staged, AchievementKind.FirstPlan).Tier).IsEqualTo(1);
        await Assert.That(Find(staged, AchievementKind.PlanConfirmed).Tier).IsEqualTo(0);
        await Assert.That(Find(confirmed, AchievementKind.PlanConfirmed).Tier).IsEqualTo(1);
    }
}
