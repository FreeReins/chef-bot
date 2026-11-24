using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Items put on the shopping list by hand, which need no plan.</summary>
public class ManualShoppingTests
{
    /// <summary>A Saturday morning, so staging plans the week that starts the next day.</summary>
    private static readonly DateTimeOffset Saturday = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static TestCore WithLibrary()
    {
        var core = new TestCore();
        core.Clock.Now = Saturday;
        core.Recipes.Rows.AddRange(TestData.Library);
        return core;
    }

    private static SaveGroceryStoresRequest Stores(params GroceryStoreDetails[] stores) => new(stores);

    [Test]
    public async Task Items_can_be_added_without_a_plan()
    {
        var core = new TestCore();

        var list = await core.PlanManager().AddManualItemAsync(new NewShoppingItem("Cod", 1, "lb"), default);

        var item = list.Single();
        await Assert.That(item.Name).IsEqualTo("Cod");
        await Assert.That(item.Quantity).IsEqualTo(1m);
        await Assert.That(item.Unit).IsEqualTo("lb");
        await Assert.That(item.Group).IsEqualTo(IngredientGroup.Protein);
        await Assert.That(item.Area).IsEqualTo(StoreArea.MeatAndSeafood);
        await Assert.That(core.Plans.Rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Adding_the_same_item_again_adds_to_its_amount()
    {
        var plans = new TestCore().PlanManager();

        await plans.AddManualItemAsync(new NewShoppingItem("cod", 1, "lb"), default);
        var list = await plans.AddManualItemAsync(new NewShoppingItem("Cod", 2, "lb"), default);

        await Assert.That(list.Count).IsEqualTo(1);
        await Assert.That(list.Single().Quantity).IsEqualTo(3m);
    }

    [Test]
    public async Task A_different_unit_is_a_separate_line()
    {
        var plans = new TestCore().PlanManager();

        await plans.AddManualItemAsync(new NewShoppingItem("cod", 1, "lb"), default);
        var list = await plans.AddManualItemAsync(new NewShoppingItem("cod", 2), default);

        await Assert.That(list.Count).IsEqualTo(2);
    }

    [Test]
    public async Task An_item_needs_a_name_and_a_positive_amount()
    {
        var plans = new TestCore().PlanManager();

        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.AddManualItemAsync(new NewShoppingItem(" "), default))).IsNotNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => plans.AddManualItemAsync(new NewShoppingItem("milk", 0), default))).IsNotNull();
    }

    [Test]
    public async Task Items_go_to_the_main_store_and_can_be_moved()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        await plans.SaveStoresAsync(Stores(new("Hy-Vee", true, null, false, null), new("Costco", false, null, false, null)), default);
        var added = await plans.AddManualItemAsync(new NewShoppingItem("cod"), default);
        await Assert.That(added.Single().Store).IsEqualTo("Hy-Vee");

        var moved = await plans.SetManualItemStoreAsync(added.Single().Id, new ShoppingStoreRequest("Costco"), default);

        await Assert.That(moved.Single().Store).IsEqualTo("Costco");
        await Assert.That(core.Settings.Stores.Single(store => store.Name == "Costco").Items).Contains("white fish");
    }

    [Test]
    public async Task Buying_an_item_puts_it_in_the_pantry_even_with_only_a_staged_plan()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        await plans.StageAsync(new StagePlanRequest(), default);
        var added = await plans.AddManualItemAsync(new NewShoppingItem("eggs", 12), default);

        var left = await plans.PurchaseManualItemAsync(added.Single().Id, new PurchaseDetails(), default);

        await Assert.That(left.Count).IsEqualTo(0);
        var stocked = core.Pantry.Rows.Single();
        await Assert.That(stocked.CatalogName).IsEqualTo("egg");
        await Assert.That(stocked.Quantity).IsEqualTo(12m);
        await Assert.That(stocked.ExpiresOn).IsEqualTo(DateOnly.FromDateTime(Saturday.Date).AddDays(28));
    }

    [Test]
    public async Task Buying_something_a_plan_needs_takes_it_off_the_plan_list()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var staged = await plans.StageAsync(new StagePlanRequest(), default);
        var needed = staged.Shopping.First();
        var added = await plans.AddManualItemAsync(new NewShoppingItem(needed.Name), default);

        await plans.PurchaseManualItemAsync(added.Single().Id, new PurchaseDetails(), default);
        await core.HandleEventsAsync();
        var plan = await plans.GetAsync(staged.Id, default);

        await Assert.That(plan.Shopping.Any(item => item.Name == needed.Name)).IsFalse();
    }

    [Test]
    public async Task Buying_everything_on_a_confirmed_plan_buys_the_hand_added_items_too()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var staged = await plans.StageAsync(new StagePlanRequest(), default);
        await plans.ConfirmAsync(staged.Id, default);
        await plans.AddManualItemAsync(new NewShoppingItem("oats"), default);

        var plan = await plans.PurchaseAllAsync(staged.Id, new PurchaseAllRequest(), default);

        await Assert.That(core.ShoppingList.Rows.Count).IsEqualTo(0);
        await Assert.That(core.Pantry.Rows.Any(item => item.Key == "oats")).IsTrue();
        await Assert.That(plan.Shopping.All(item => item.IsPurchased)).IsTrue();
    }

    [Test]
    public async Task Buying_everything_at_one_store_leaves_the_other_stores_items()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        await plans.SaveStoresAsync(Stores(new("Hy-Vee", true, null, false, null), new("Costco", false, null, false, null)), default);
        var staged = await plans.StageAsync(new StagePlanRequest(), default);
        await plans.ConfirmAsync(staged.Id, default);
        var moved = await plans.SetShoppingStoreAsync(staged.Id, staged.Shopping.First().Id, new ShoppingStoreRequest("Costco"), default);
        var atCostco = moved.Shopping.Where(item => item.Store == "Costco").Select(item => item.Id).ToHashSet();
        await plans.AddManualItemAsync(new NewShoppingItem("oats"), default);

        var plan = await plans.PurchaseAllAsync(staged.Id, new PurchaseAllRequest(Store: " costco "), default);

        await Assert.That(atCostco.Count).IsGreaterThan(0);
        await Assert.That(plan.Shopping.Where(item => atCostco.Contains(item.Id)).All(item => item.IsPurchased)).IsTrue();
        await Assert.That(plan.Shopping.Where(item => !atCostco.Contains(item.Id)).Any(item => item.IsPurchased)).IsFalse();
        await Assert.That(core.ShoppingList.Rows.Count).IsEqualTo(1);
        await Assert.That(core.Pantry.Rows.Count).IsEqualTo(atCostco.Count);
    }

    [Test]
    public async Task Buying_hand_added_items_at_one_store_keeps_the_rest()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        await plans.SaveStoresAsync(Stores(new("Hy-Vee", true, null, false, null), new("Costco", false, null, false, null)), default);
        await plans.AddManualItemAsync(new NewShoppingItem("oats"), default);
        var added = await plans.AddManualItemAsync(new NewShoppingItem("cod"), default);
        await plans.SetManualItemStoreAsync(added.Single(item => item.Name == "cod").Id, new ShoppingStoreRequest("Costco"), default);

        var left = await plans.PurchaseAllManualItemsAsync(new PurchaseAllRequest(Store: "costco"), default);

        await Assert.That(left.Single().Name).IsEqualTo("oats");
        await Assert.That(core.Pantry.Rows.Single().Key).IsEqualTo("cod");
    }

    [Test]
    public async Task Buying_every_hand_added_item_empties_the_list()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        await plans.AddManualItemAsync(new NewShoppingItem("oats"), default);
        await plans.AddManualItemAsync(new NewShoppingItem("kale", 1, "bunch"), default);

        var left = await plans.PurchaseAllManualItemsAsync(new PurchaseAllRequest(), default);

        await Assert.That(left.Count).IsEqualTo(0);
        await Assert.That(core.Pantry.Rows.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Removing_an_item_does_not_stock_it()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        var added = await plans.AddManualItemAsync(new NewShoppingItem("oats"), default);

        var left = await plans.RemoveManualItemAsync(added.Single().Id, default);

        await Assert.That(left.Count).IsEqualTo(0);
        await Assert.That(core.Pantry.Rows.Count).IsEqualTo(0);
        await Assert.That(await TestData.Caught<NotFoundException>(() => plans.RemoveManualItemAsync(added.Single().Id, default))).IsNotNull();
    }
}
