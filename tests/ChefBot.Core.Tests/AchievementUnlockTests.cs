using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class AchievementUnlockTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static Achievement Badge(AchievementKind kind, int tier) => new(kind, tier, 3, 0, null, false);

    private static AchievementsResponse Reached(int rank, params Achievement[] achievements) =>
        new(0, rank, 0, null, achievements.Where(a => a.Tiers == 1).ToList(), achievements.Where(a => a.Tiers == 3).ToList(), []);

    private static AchievementUnlock Kept(AchievementKind? kind, int tier) => new() { Kind = kind, Tier = tier, EarnedAt = Now.AddDays(-1) };

    private static (TestCore Core, UserAccount Cook) Kitchen()
    {
        var core = new TestCore();
        core.Clock.Now = Now;
        core.Recipes.Rows.AddRange(TestData.Library);
        var cook = new UserAccount { Username = "sam", DisplayName = "Sam" };
        core.Accounts.Users.Add(cook);
        return (core, cook);
    }

    [Test]
    public async Task Jumping_tiers_unlocks_each_tier_passed()
    {
        var fresh = AchievementEngine.Unlocks(Reached(0, Badge(AchievementKind.Cookbook, 2)), [Kept(null, 0)], Now, null, quiet: false);

        await Assert.That(string.Join(", ", fresh.Select(unlock => $"{unlock.Kind} {unlock.Tier}"))).IsEqualTo("Cookbook 1, Cookbook 2");
    }

    [Test]
    public async Task A_tier_that_drops_and_comes_back_is_not_new()
    {
        var kept = new[] { Kept(null, 0), Kept(AchievementKind.Cookbook, 1), Kept(AchievementKind.Cookbook, 2) };

        var dropped = AchievementEngine.Unlocks(Reached(0, Badge(AchievementKind.Cookbook, 1)), kept, Now, null, quiet: false);
        var back = AchievementEngine.Unlocks(Reached(0, Badge(AchievementKind.Cookbook, 2)), kept, Now, null, quiet: false);
        var further = AchievementEngine.Unlocks(Reached(0, Badge(AchievementKind.Cookbook, 3)), kept, Now, null, quiet: false);

        await Assert.That(dropped.Count).IsEqualTo(0);
        await Assert.That(back.Count).IsEqualTo(0);
        await Assert.That(further.Single().Tier).IsEqualTo(3);
    }

    [Test]
    public async Task Ranks_unlock_once_each_and_optional_steps_never_do()
    {
        var guest = new Achievement(AchievementKind.FirstGuest, 1, 1, 1, null, true);

        var fresh = AchievementEngine.Unlocks(Reached(2, guest), [Kept(null, 0)], Now, null, quiet: false);

        await Assert.That(string.Join(", ", fresh.Select(unlock => $"{unlock.Kind?.ToString() ?? "Rank"} {unlock.Tier}"))).IsEqualTo("Rank 1, Rank 2");
    }

    [Test]
    public async Task The_first_check_records_what_was_reached_without_announcing_it()
    {
        var (core, _) = Kitchen();

        var announced = await core.RecipeManager().CheckAchievementsAsync(null, default);

        await Assert.That(announced.Count).IsEqualTo(0);
        await Assert.That(core.Achievements.Rows.Count).IsGreaterThan(1);
        await Assert.That(core.Achievements.Rows.All(unlock => unlock.Quiet)).IsTrue();
        await Assert.That(core.Achievements.Rows.Any(unlock => unlock is { Kind: null, Tier: 0 })).IsTrue();
        await Assert.That(core.Messages.Published.OfType<AchievementsUnlocked>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task A_new_kitchen_hears_about_its_very_first_step()
    {
        var core = new TestCore();
        core.Clock.Now = Now;
        await core.RecipeManager().CheckAchievementsAsync(null, default);
        core.Recipes.Rows.Add(TestData.Library[0]);

        var announced = await core.RecipeManager().CheckAchievementsAsync(null, default);

        await Assert.That(announced.Single().Kind).IsEqualTo(AchievementKind.FirstRecipe);
    }

    [Test]
    public async Task A_new_tier_is_saved_and_announced_once_however_often_the_check_runs()
    {
        var (core, cook) = Kitchen();
        await core.RecipeManager().CheckAchievementsAsync(null, default);
        await core.RecipeManager().AddFavouriteAsync(TestData.Library[0].Id, cook.Id, default);

        var first = await core.RecipeManager().CheckAchievementsAsync(Guid.NewGuid(), default);
        var again = await core.RecipeManager().CheckAchievementsAsync(Guid.NewGuid(), default);

        await Assert.That(first.Single().Kind).IsEqualTo(AchievementKind.FirstFavourite);
        await Assert.That(again.Count).IsEqualTo(0);
        await Assert.That(core.Messages.Published.OfType<AchievementsUnlocked>().Single().Unlocks.Single().Kind)
            .IsEqualTo(AchievementKind.FirstFavourite);
    }

    [Test]
    public async Task Each_person_is_shown_a_new_tier_until_they_say_they_saw_it()
    {
        var (core, cook) = Kitchen();
        var other = new UserAccount { Username = "alex", DisplayName = "Alex" };
        core.Accounts.Users.Add(other);
        await core.RecipeManager().CheckAchievementsAsync(null, default);
        await core.RecipeManager().AddFavouriteAsync(TestData.Library[0].Id, cook.Id, default);
        var unlock = (await core.RecipeManager().CheckAchievementsAsync(null, default)).Single();

        var before = await core.RecipeManager().GetAchievementsAsync(cook.Id, default);
        await core.RecipeManager().MarkAchievementsSeenAsync(cook.Id, new AchievementsSeenRequest(unlock.Id), default);
        var after = await core.RecipeManager().GetAchievementsAsync(cook.Id, default);
        var otherView = await core.RecipeManager().GetAchievementsAsync(other.Id, default);

        await Assert.That(before.Unseen.Single().Id).IsEqualTo(unlock.Id);
        await Assert.That(after.Unseen.Count).IsEqualTo(0);
        await Assert.That(otherView.Unseen.Single().Id).IsEqualTo(unlock.Id);
    }

    [Test]
    public async Task Favouriting_announces_it_in_the_same_commit()
    {
        var (core, cook) = Kitchen();

        await core.RecipeManager().AddFavouriteAsync(TestData.Library[0].Id, cook.Id, default);

        await Assert.That(core.Messages.Published.OfType<RecipeFavourited>().Single().UserId).IsEqualTo(cook.Id);
        await Assert.That(core.Messages.Commits).IsEqualTo(1);
    }
}
