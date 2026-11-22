using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class HistoryTests
{
    private static readonly HistoryEngine Storyteller = new();

    private static readonly DateOnly March = new(2026, 3, 1);

    private static FoodMoment Cooked(Recipe recipe, DateOnly on, MealSlot slot = MealSlot.Dinner) =>
        new() { Kind = FoodMomentKind.Cooked, RecipeId = recipe.Id, Title = recipe.Name, Slot = slot, OccurredOn = on, MealId = Guid.NewGuid() };

    private static FoodMoment Added(string name, DateOnly on) =>
        new() { Kind = FoodMomentKind.RecipeAdded, RecipeId = Guid.NewGuid(), Title = name, OccurredOn = on };

    private static HistoryResponse Tell(IEnumerable<FoodMoment> moments, int months = HistoryEngine.MaxMonths, DateOnly? before = null) =>
        Storyteller.Tell(moments.ToList(), [], TestData.Library, new HistoryRequest(before, months), TimeZoneInfo.Utc);

    private static List<HistoryMoment> All(HistoryResponse history) => history.Chapters.SelectMany(chapter => chapter.Moments).ToList();

    /// <summary>The chapters' months in order, to compare as text.</summary>
    private static string Months(HistoryResponse history) => string.Join(", ", history.Chapters.Select(chapter => chapter.Month.ToString("yyyy-MM")));

    [Test]
    public async Task A_first_cook_is_a_debut_and_later_ones_share_the_weeks_plates()
    {
        var history = Tell([Cooked(TestData.Stew, March), Cooked(TestData.Stew, March.AddDays(2)), Cooked(TestData.Soup, March.AddDays(3)), Cooked(TestData.Soup, March.AddDays(4))]);

        var moments = All(history);
        await Assert.That(moments.Count(moment => moment.Kind == HistoryMomentKind.Debut)).IsEqualTo(2);
        var plates = moments.Single(moment => moment.Kind == HistoryMomentKind.WeekOfPlates);
        await Assert.That(plates.Dishes.Select(dish => dish.Title)).IsEquivalentTo([TestData.Stew.Name, TestData.Soup.Name]);
        await Assert.That(plates.Dishes[0].RecipeId).IsEqualTo(TestData.Stew.Id);
        await Assert.That(history.TotalDinners).IsEqualTo(4);
        await Assert.That(history.Since).IsEqualTo(March);
    }

    [Test]
    public async Task The_fifth_and_tenth_cooks_make_a_fan_favourite()
    {
        var cooks = Enumerable.Range(0, 10).Select(week => Cooked(TestData.Tacos, March.AddDays(7 * week)));

        var favourites = All(Tell(cooks)).Where(moment => moment.Kind == HistoryMomentKind.FanFavourite).ToList();

        await Assert.That(string.Join(", ", favourites.Select(moment => moment.Count))).IsEqualTo("10, 5");
    }

    [Test]
    public async Task Cooking_a_recipe_again_after_a_long_rest_is_a_comeback()
    {
        var history = Tell([Cooked(TestData.Bowls, March), Cooked(TestData.Bowls, March.AddDays(120))]);

        var comeback = All(history).Single(moment => moment.Kind == HistoryMomentKind.Comeback);
        await Assert.That(comeback.Count).IsEqualTo(120);
        await Assert.That(comeback.Title).IsEqualTo(TestData.Bowls.Name);
    }

    [Test]
    public async Task The_hundredth_dinner_is_a_milestone_but_lunches_dont_count()
    {
        var dinners = Enumerable.Range(0, 100).Select(day => Cooked(TestData.Soup, March.AddDays(day)));
        var lunches = Enumerable.Range(0, 30).Select(day => Cooked(TestData.Stew, March.AddDays(day), MealSlot.Lunch));

        var history = Tell(dinners.Concat(lunches));

        var milestones = All(history).Where(moment => moment.Kind == HistoryMomentKind.DinnerMilestone).Select(moment => moment.Count);
        await Assert.That(string.Join(", ", milestones)).IsEqualTo("100, 50");
        await Assert.That(history.TotalDinners).IsEqualTo(100);
    }

    [Test]
    public async Task Many_recipes_added_on_one_day_arrive_as_a_stampede()
    {
        var imported = Enumerable.Range(1, 8).Select(number => Added($"Imported {number}", March));

        var moments = All(Tell([.. imported, Added("Lone Pie", March.AddDays(1))]));

        var stampede = moments.Single(moment => moment.Kind == HistoryMomentKind.RecipeStampede);
        await Assert.That(stampede.Count).IsEqualTo(8);
        await Assert.That(stampede.Dishes.Count).IsEqualTo(6);
        await Assert.That(moments.Single(moment => moment.Kind == HistoryMomentKind.RecipeAdded).Title).IsEqualTo("Lone Pie");
    }

    [Test]
    public async Task Chapters_are_months_newest_first_with_their_top_dish_and_tag()
    {
        var tagged = TestData.NewRecipe("Lentil Soup", RecipeCourse.Main, false, "1 cup lentils");
        tagged.Tags = ["Soup"];
        var soupy = TestData.NewRecipe("Leek Soup", RecipeCourse.Main, false, "2 leeks");
        soupy.Tags = ["soup"];
        var recipes = new List<Recipe>(TestData.Library) { tagged, soupy };

        var history = Storyteller.Tell(
            [Cooked(TestData.Stew, March.AddMonths(-1)), Cooked(tagged, March), Cooked(soupy, March.AddDays(1)), Cooked(tagged, March.AddDays(9))],
            [],
            recipes,
            new HistoryRequest(null, 0),
            TimeZoneInfo.Utc);

        await Assert.That(Months(history)).IsEqualTo("2026-03, 2026-02");
        var chapter = history.Chapters[0];
        await Assert.That(chapter.Dinners).IsEqualTo(3);
        await Assert.That(chapter.TopDish).IsEqualTo("Lentil Soup");
        await Assert.That(chapter.TopDishCooks).IsEqualTo(2);
        await Assert.That(chapter.TopTag).IsEqualTo("Soup");
        await Assert.That(chapter.Moments[0].At >= chapter.Moments[^1].At).IsTrue();
    }

    [Test]
    public async Task Older_months_come_a_page_at_a_time()
    {
        var cooks = Enumerable.Range(0, 5).Select(month => Cooked(TestData.Soup, March.AddMonths(month)));

        var first = Tell(cooks, months: 2);
        var second = Tell(cooks, months: 2, before: first.OlderBefore);
        var last = Tell(cooks, months: 2, before: second.OlderBefore);

        await Assert.That(Months(first)).IsEqualTo("2026-07, 2026-06");
        await Assert.That(Months(second)).IsEqualTo("2026-05, 2026-04");
        await Assert.That(last.Chapters.Single().Month).IsEqualTo(March);
        await Assert.That(last.OlderBefore).IsNull();
    }

    [Test]
    public async Task An_archived_recipe_is_still_told_but_not_linked()
    {
        var retired = TestData.NewRecipe("Beige Casserole", RecipeCourse.Main, false, "1 can soup");
        retired.IsArchived = true;

        var history = Storyteller.Tell([Cooked(retired, March)], [], [retired], new HistoryRequest(null, 0), TimeZoneInfo.Utc);

        var debut = All(history).Single();
        await Assert.That(debut.Title).IsEqualTo("Beige Casserole");
        await Assert.That(debut.RecipeId).IsNull();
    }

    [Test]
    public async Task Cooking_is_kept_undoing_it_takes_it_back_and_clearing_the_plan_keeps_it()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        var plans = core.PlanManager();
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        var stew = plan.Meals.Single(meal => meal.Title == TestData.Stew.Name && !meal.IsLeftover);

        await plans.SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Cooked), default);
        await core.HandleEventsAsync();
        var cooked = core.History.Rows.Single();
        await Assert.That(cooked.Kind).IsEqualTo(FoodMomentKind.Cooked);
        await Assert.That(cooked.RecipeId).IsEqualTo(TestData.Stew.Id);
        await Assert.That(cooked.OccurredOn).IsEqualTo(new DateOnly(2026, 11, 14));
        await Assert.That(cooked.OccurredAt).IsEqualTo(core.Clock.Now);

        await plans.SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Planned), default);
        await core.HandleEventsAsync();
        await Assert.That(core.History.Rows).IsEmpty();

        await plans.SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Cooked), default);
        await plans.ClearAsync(plan.Id, default);
        await core.HandleEventsAsync();
        await Assert.That(core.History.Rows.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_dinner_marked_cooked_after_its_day_keeps_the_day_but_no_time()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        var plans = core.PlanManager();
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        var stew = plan.Meals.Single(meal => meal.Title == TestData.Stew.Name && !meal.IsLeftover);

        core.Clock.Now = stew.Date!.Value.AddDays(2).ToDateTime(new TimeOnly(9, 0));
        await plans.SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Cooked), default);
        await core.HandleEventsAsync();

        var cooked = core.History.Rows.Single();
        await Assert.That(cooked.OccurredOn).IsEqualTo(stew.Date!.Value);
        await Assert.That(cooked.OccurredAt).IsNull();
    }

    [Test]
    public async Task A_moment_keeps_its_time()
    {
        var at = new DateTimeOffset(2026, 3, 1, 18, 42, 0, TimeSpan.Zero);
        var cook = Cooked(TestData.Stew, March);
        cook.OccurredAt = at;

        var debut = All(Tell([cook])).Single(moment => moment.Kind == HistoryMomentKind.Debut);

        await Assert.That(debut.At).IsEqualTo(at);
    }

    [Test]
    public async Task Cooking_a_favourite_remembers_whose_it_was_then()
    {
        var core = new TestCore();
        core.Clock.Now = new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
        core.Recipes.Rows.AddRange(TestData.Library);
        var james = new UserAccount { Username = "james", DisplayName = "James" };
        core.Accounts.Users.Add(james);
        var recipes = core.RecipeManager();
        await recipes.AddFavouriteAsync(TestData.Stew.Id, james.Id, default);
        var plans = core.PlanManager();
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        var stew = plan.Meals.First(meal => meal.Title == TestData.Stew.Name && !meal.IsLeftover);
        core.Clock.Now = stew.Date!.Value.ToDateTime(new TimeOnly(18, 0));

        await plans.SetMealStatusAsync(plan.Id, stew.Id, new MealStatusRequest(MealStatus.Cooked), default);
        await recipes.RemoveFavouriteAsync(TestData.Stew.Id, james.Id, default);
        await core.HandleEventsAsync();

        await Assert.That(string.Join(", ", core.History.Rows.Single().FavouredBy)).IsEqualTo("James");
    }

    [Test]
    public async Task An_everyday_cook_of_a_favourite_gets_its_own_moment()
    {
        var debut = Cooked(TestData.Stew, March);
        var again = Cooked(TestData.Stew, March.AddDays(2));
        again.FavouredBy = ["Jamie", "Chris"];
        var plain = Cooked(TestData.Stew, March.AddDays(3));

        var moments = All(Tell([debut, again, plain]));

        var favourite = moments.Single(moment => moment.Kind == HistoryMomentKind.FavouriteCooked);
        await Assert.That(string.Join(", ", favourite.FavouredBy)).IsEqualTo("Jamie, Chris");
        await Assert.That(favourite.RecipeId).IsEqualTo(TestData.Stew.Id);
        var plates = moments.Single(moment => moment.Kind == HistoryMomentKind.WeekOfPlates);
        await Assert.That(plates.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_favourites_debut_stays_a_debut_and_names_whose_it_is()
    {
        var debut = Cooked(TestData.Stew, March);
        debut.FavouredBy = ["Jamie"];

        var moments = All(Tell([debut]));

        var told = moments.Single();
        await Assert.That(told.Kind).IsEqualTo(HistoryMomentKind.Debut);
        await Assert.That(string.Join(", ", told.FavouredBy)).IsEqualTo("Jamie");
    }

    [Test]
    public async Task Moments_not_about_a_cook_name_no_favourites()
    {
        var moments = All(Tell([Added("Pho", March), Cooked(TestData.Stew, March), Cooked(TestData.Stew, March.AddDays(1))]));

        await Assert.That(moments.All(moment => moment.FavouredBy.Count == 0)).IsTrue();
    }

    [Test]
    public async Task Adding_and_archiving_recipes_are_kept()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();

        var pie = await recipes.CreateAsync(new RecipeDraft("Lone Pie", RecipeCourse.Main, ["1 pie crust"]), default);
        await recipes.ArchiveAsync(pie.Id, default);
        await recipes.ArchiveAsync(pie.Id, default);
        await core.HandleEventsAsync();

        await Assert.That(core.History.Rows.Select(row => row.Kind)).IsEquivalentTo([FoodMomentKind.RecipeAdded, FoodMomentKind.RecipeRetired]);
        var history = await recipes.GetHistoryAsync(new HistoryRequest(null, 0), default);
        await Assert.That(string.Join(", ", All(history).Select(moment => moment.Kind))).IsEqualTo("RecipeRetired, RecipeAdded");
    }

    [Test]
    public async Task Moments_of_one_day_go_latest_first_whatever_their_kind()
    {
        var morning = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var added = Added("Pancakes", March);
        added.OccurredAt = morning;
        var cooked = Cooked(TestData.Stew, March);
        cooked.OccurredAt = morning.AddHours(4);
        var retired = new FoodMoment { Kind = FoodMomentKind.RecipeRetired, Title = "Old pie", OccurredOn = March, OccurredAt = morning.AddHours(10) };
        var undated = new FoodMoment { Kind = FoodMomentKind.Haul, Count = 3, OccurredOn = March };

        var history = Tell([undated, retired, added, cooked]);

        await Assert.That(string.Join(", ", All(history).Select(moment => moment.Kind))).IsEqualTo("RecipeRetired, Debut, RecipeAdded, Haul");
    }

    [Test]
    public async Task Setting_up_opens_the_kitchen()
    {
        var core = new TestCore();

        await core.AccountManager().CreateFirstAdminAsync(new NewAccount("jamie", "Jamie", "correct horse battery"), default);
        await core.HandleEventsAsync();

        var opened = core.History.Rows.Single();
        await Assert.That(opened.Kind).IsEqualTo(FoodMomentKind.KitchenOpened);
        await Assert.That(opened.Title).IsEqualTo("Jamie");
    }
}
