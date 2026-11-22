using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Favourites in planning: a soft preference set per plan style, and a rule kind, both behind avoiding repeats.</summary>
public class FavouritePlanningTests
{
    /// <summary>Twelve dinners that score the same on everything else, so only favourites tell them apart.</summary>
    private static readonly IReadOnlyList<Recipe> Plain = Enumerable.Range(1, 12)
        .Select(number => TestData.NewRecipe($"Plain Dinner {number}", RecipeCourse.Main, false, "1 cup water"))
        .ToList();

    private static readonly Recipe First = Plain[0];

    private static readonly Recipe Second = Plain[1];

    private static PlanStyle Style(FavouriteLean lean, params PlanRule[] rules)
    {
        var style = PlanStyle.Classic();
        style.Rules = [.. rules];
        style.Favourites = lean;
        style.Breakfasts = style.Snacks = style.Sweets = 0;
        return style;
    }

    private static PlanDraft Plan(PlanStyle style, int seed, IReadOnlySet<Guid>? recent = null) =>
        TestData.Planning.Plan(new PlanRequest(
            TestData.Sunday, Plain, recent ?? new HashSet<Guid>(), PantrySnapshot.Empty, DateTimeOffset.UnixEpoch,
            Seed: seed, Style: style, Fans: new Dictionary<Guid, int> { [First.Id] = 1, [Second.Id] = 2 }));

    private static bool Has(PlanDraft draft, Recipe recipe) => TestData.Dinners(draft.Plan).Any(dinner => dinner.RecipeId == recipe.Id);

    [Test]
    public async Task A_style_that_leans_on_favourites_plans_them()
    {
        foreach (var seed in Enumerable.Range(1, 8))
        {
            var draft = Plan(Style(FavouriteLean.ALot), seed);

            await Assert.That(Has(draft, First) && Has(draft, Second)).IsTrue();
        }
    }

    [Test]
    public async Task With_favourites_off_they_are_planned_no_more_than_others()
    {
        var plannedBoth = Enumerable.Range(1, 8).Count(seed => Plan(Style(FavouriteLean.Off), seed) is var draft && Has(draft, First) && Has(draft, Second));

        await Assert.That(plannedBoth).IsLessThan(8);
    }

    [Test]
    public async Task A_favourite_planned_recently_is_still_left_out()
    {
        foreach (var seed in Enumerable.Range(1, 8))
        {
            var draft = Plan(Style(FavouriteLean.ALot), seed, recent: new HashSet<Guid> { First.Id, Second.Id });

            await Assert.That(Has(draft, First) || Has(draft, Second)).IsFalse();
            await Assert.That(draft.Warnings).IsEmpty();
        }
    }

    [Test]
    public async Task More_fans_count_for_more_but_each_adds_less()
    {
        var fans = new Dictionary<Guid, int> { [First.Id] = 1, [Second.Id] = 2 };
        var scorer = new WeekScorer(TestData.Catalog, TestData.Options, PantrySnapshot.Empty, 11, fans: fans, favouriteWeight: 2);

        await Assert.That(scorer.ScoreAlone(Plain[2])).IsEqualTo(0);
        await Assert.That(scorer.ScoreAlone(First)).IsEqualTo(2);
        await Assert.That(scorer.ScoreAlone(Second)).IsEqualTo(3);
    }

    [Test]
    public async Task A_rule_can_ask_for_favourite_dinners()
    {
        var rule = new PlanRule { Limit = RuleLimit.AtLeast, Count = 2, Slot = MealSlot.Dinner, Match = RecipeMatchKind.Favourite };

        foreach (var seed in Enumerable.Range(1, 5))
        {
            var draft = Plan(Style(FavouriteLean.Off, rule), seed);

            await Assert.That(Has(draft, First) && Has(draft, Second)).IsTrue();
            await Assert.That(draft.Warnings).IsEmpty();
        }
    }

    [Test]
    public async Task A_favourites_rule_gives_way_rather_than_repeat_a_recent_meal()
    {
        var rule = new PlanRule { Limit = RuleLimit.AtLeast, Count = 2, Slot = MealSlot.Dinner, Match = RecipeMatchKind.Favourite };

        var draft = Plan(Style(FavouriteLean.Off, rule), seed: 1, recent: new HashSet<Guid> { First.Id });

        await Assert.That(Has(draft, First)).IsFalse();
        await Assert.That(Has(draft, Second)).IsTrue();
        await Assert.That(draft.Warnings.Single()).Contains("Only 1 of 2 dinners could be someone's favourite");
    }

    [Test]
    public async Task The_style_setting_is_kept_when_saved_and_read_from_starter_styles()
    {
        var details = PlanStyle.Classic().ToResponse() with { Id = null, Favourites = FavouriteLean.ALot };
        var saved = new[] { details }.ToStyles([]);
        await Assert.That(saved.Single().Favourites).IsEqualTo(FavouriteLean.ALot);

        var starters = KnownPlanStyleAccess.FromJson("""
            { "styles": [ { "name": "Usual" }, { "name": "Treats", "favourites": "ALot" } ] }
            """);
        await Assert.That(starters[0].Favourites).IsEqualTo(FavouriteLean.ALittle);
        await Assert.That(starters[1].Favourites).IsEqualTo(FavouriteLean.ALot);
    }
}
