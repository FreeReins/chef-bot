using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

public class StoreTests
{
    /// <summary>A Saturday morning, so staging plans the week that starts the next day.</summary>
    private static readonly DateTimeOffset Saturday = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static GroceryStore MainStore(string name = "Hy-Vee") => new() { Name = name, IsMain = true };

    private static GroceryStore Selling(string name, params string[] items) => new() { Name = name, Items = [.. items] };

    private static GroceryStore Market(params int[] months) => new() { Name = "Farmers market", SeasonalProduce = true, Months = [.. months] };

    /// <summary>One dinner of each recipe in <see cref="TestData.Sunday"/>'s week, in November.</summary>
    private static MealPlan PlanOf(params Recipe[] recipes)
    {
        var plan = new MealPlan { WeekStart = TestData.Sunday };
        foreach (var recipe in recipes)
        {
            plan.Meals.Add(TestData.Meal(recipe, MealSlot.Dinner, TestData.Sunday, portions: 4));
        }

        return plan;
    }

    private static string? StoreOf(ShoppingDraft draft, string key) => draft.Items.FirstOrDefault(item => item.Key == key)?.Store;

    private static TestCore WithLibrary()
    {
        var core = new TestCore();
        core.Clock.Now = Saturday;
        core.Recipes.Rows.AddRange(TestData.Library);
        return core;
    }

    private static GroceryStoreDetails Details(string name, bool main = false, string[]? items = null, int[]? months = null) =>
        new(name, main, items, months is not null, months);

    // ---------- choosing a store ----------

    [Test]
    public async Task With_no_stores_nothing_gets_a_store()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos, TestData.Bowls), PantrySnapshot.Empty, []);

        await Assert.That(draft.Items.Count > 0).IsTrue();
        await Assert.That(draft.Items.All(item => item.Store.Length == 0)).IsTrue();
    }

    [Test]
    public async Task A_store_that_lists_an_ingredient_gets_every_cut_of_it()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Bowls), PantrySnapshot.Empty, [MainStore(), Selling("Costco", "chicken")]);

        await Assert.That(StoreOf(draft, "chicken thigh")).IsEqualTo("Costco");
        await Assert.That(StoreOf(draft, "cabbage")).IsEqualTo("Hy-Vee");
    }

    [Test]
    public async Task In_season_produce_goes_to_an_open_market()
    {
        // November: cabbage is in season, cilantro isn't, and tortillas aren't produce.
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos), PantrySnapshot.Empty, [MainStore(), Market(10, 11)]);

        await Assert.That(StoreOf(draft, "cabbage")).IsEqualTo("Farmers market");
        await Assert.That(StoreOf(draft, "cilantro")).IsEqualTo("Hy-Vee");
        await Assert.That(StoreOf(draft, "tortilla")).IsEqualTo("Hy-Vee");
    }

    [Test]
    public async Task A_closed_market_gets_nothing()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos), PantrySnapshot.Empty, [MainStore(), Market(5, 6, 7)]);

        await Assert.That(StoreOf(draft, "cabbage")).IsEqualTo("Hy-Vee");
    }

    [Test]
    public async Task Without_a_main_store_the_rest_has_no_store()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Bowls), PantrySnapshot.Empty, [Selling("Costco", "chicken")]);

        await Assert.That(StoreOf(draft, "chicken thigh")).IsEqualTo("Costco");
        await Assert.That(StoreOf(draft, "cabbage")).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Overview_lines_carry_their_food_group()
    {
        var plan = PlanOf(TestData.Bowls);
        TestData.Shopping.ApplyList(plan, TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []));

        var lines = TestData.Shopping.BuildOverview(plan, [], TestData.Sunday, 3).Ingredients;

        await Assert.That(lines.Single(line => line.Name == "cabbage").Group).IsEqualTo(IngredientGroup.Vegetable);
        await Assert.That(lines.Single(line => line.Name == "chicken thigh").Group).IsEqualTo(IngredientGroup.Protein);
    }

    [Test]
    public async Task Sending_an_ingredient_elsewhere_takes_it_off_other_stores()
    {
        var costco = Selling("Costco", "chicken", "egg");
        var butcher = Selling("Butcher");
        var thigh = TestData.Catalog.Identify("chicken thigh");

        ShoppingEngine.SendTo([costco, butcher], thigh, butcher);

        await Assert.That(costco.Items).IsEquivalentTo(["egg"]);
        await Assert.That(butcher.Items).IsEquivalentTo(["chicken"]);
    }

    // ---------- checking a list of stores ----------

    [Test]
    public async Task Only_one_store_can_be_the_main_one()
    {
        var problem = await TestData.Caught<RuleViolationException>(() => WithLibrary().PlanManager()
            .SaveStoresAsync(new SaveGroceryStoresRequest([Details("Hy-Vee", main: true), Details("Aldi", main: true)]), default));

        await Assert.That(problem is not null).IsTrue();
    }

    [Test]
    public async Task Store_names_must_differ()
    {
        var problem = await TestData.Caught<RuleViolationException>(() => WithLibrary().PlanManager()
            .SaveStoresAsync(new SaveGroceryStoresRequest([Details("Aldi"), Details(" aldi ")]), default));

        await Assert.That(problem?.Message).IsEqualTo("There are two stores called aldi. Give each store its own name.");
    }

    [Test]
    public async Task A_seasonal_store_needs_its_months()
    {
        var problem = await TestData.Caught<RuleViolationException>(() => WithLibrary().PlanManager()
            .SaveStoresAsync(new SaveGroceryStoresRequest([new GroceryStoreDetails("Market", false, null, true, [])]), default));

        await Assert.That(problem is not null).IsTrue();
    }

    // ---------- the manager ----------

    [Test]
    public async Task Saving_stores_rebuilds_open_shopping_lists()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var staged = await plans.StageAsync(new StagePlanRequest(), default);
        await Assert.That(staged.Shopping.All(item => item.Store.Length == 0)).IsTrue();

        var saved = await plans.SaveStoresAsync(new SaveGroceryStoresRequest([Details("Hy-Vee", main: true)]), default);
        await core.HandleEventsAsync();
        var plan = await plans.GetAsync(staged.Id, default);

        await Assert.That(saved.Stores.Single().Name).IsEqualTo("Hy-Vee");
        await Assert.That(plan.Shopping.All(item => item.Store == "Hy-Vee")).IsTrue();
    }

    [Test]
    public async Task Saving_tidies_items_and_drops_months_of_stores_that_are_not_seasonal()
    {
        var core = WithLibrary();

        await core.PlanManager().SaveStoresAsync(new SaveGroceryStoresRequest(
            [new GroceryStoreDetails(" Costco ", false, ["Chicken", "chicken", " "], false, [5])]), default);

        var costco = core.Settings.Stores.Single();
        await Assert.That(costco.Name).IsEqualTo("Costco");
        await Assert.That(costco.Items).IsEquivalentTo(["chicken"]);
        await Assert.That(costco.Months.Count).IsEqualTo(0);
    }

    [Test]
    public async Task The_store_list_offers_known_stores_and_catalog_ingredients_without_staples()
    {
        var stores = await WithLibrary().PlanManager().GetStoresAsync(default);

        await Assert.That(stores.Stores.Count).IsEqualTo(0);
        await Assert.That(stores.Known.Select(store => store.Name)).IsEquivalentTo(["Costco", "Farmers market"]);
        await Assert.That(stores.Ingredients.Contains("cabbage")).IsTrue();
        await Assert.That(stores.Ingredients.Contains("salt")).IsFalse();
    }

    [Test]
    public async Task Moving_an_item_remembers_the_store_and_updates_the_list()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        await plans.SaveStoresAsync(new SaveGroceryStoresRequest([Details("Hy-Vee", main: true), Details("Costco")]), default);
        var staged = await plans.StageAsync(new StagePlanRequest(), default);
        var item = staged.Shopping.First(line => line.Store == "Hy-Vee");

        var moved = await plans.SetShoppingStoreAsync(staged.Id, item.Id, new ShoppingStoreRequest("costco"), default);

        await Assert.That(moved.Shopping.Where(line => line.Name == item.Name).All(line => line.Store == "Costco")).IsTrue();
        await Assert.That(core.Settings.Stores.Single(store => store.Name == "Costco").Items.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Moving_an_item_to_no_store_sends_it_back_to_the_main_store()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        await plans.SaveStoresAsync(new SaveGroceryStoresRequest([Details("Hy-Vee", main: true), Details("Costco")]), default);
        var staged = await plans.StageAsync(new StagePlanRequest(), default);
        var item = staged.Shopping.First();
        await plans.SetShoppingStoreAsync(staged.Id, item.Id, new ShoppingStoreRequest("Costco"), default);
        var movedId = (await plans.GetAsync(staged.Id, default)).Shopping.First(line => line.Name == item.Name).Id;

        var back = await plans.SetShoppingStoreAsync(staged.Id, movedId, new ShoppingStoreRequest(""), default);

        await Assert.That(back.Shopping.First(line => line.Name == item.Name).Store).IsEqualTo("Hy-Vee");
        await Assert.That(core.Settings.Stores.All(store => store.Items.Count == 0)).IsTrue();
    }

    [Test]
    public async Task Moving_an_item_to_an_unknown_store_is_refused()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var staged = await plans.StageAsync(new StagePlanRequest(), default);

        var problem = await TestData.Caught<NotFoundException>(() =>
            plans.SetShoppingStoreAsync(staged.Id, staged.Shopping[0].Id, new ShoppingStoreRequest("Nowhere"), default));

        await Assert.That(problem is not null).IsTrue();
    }

    // ---------- the shipped files ----------

    [Test]
    public async Task Every_known_store_item_is_in_the_shipped_catalog()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "config");
        var catalog = IngredientCatalog.FromJson(File.ReadAllText(Path.Combine(folder, "ingredients.json")));
        var known = KnownStoreAccess.FromJson(File.ReadAllText(Path.Combine(folder, "stores.json")));

        var missing = known.SelectMany(store => store.Items.Where(item => catalog.Get(item) is null).Select(item => $"{store.Name}: {item}")).ToList();

        await Assert.That(known.Count > 20).IsTrue();
        await Assert.That(missing).IsEmpty();
        await Assert.That(known.Where(store => store.SeasonalProduce).All(store => store.Months.Count > 0)).IsTrue();
    }
}
