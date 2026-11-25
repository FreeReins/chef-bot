using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Setting up to-do apps and sending the shopping list to them.</summary>
public class ShoppingTargetTests
{
    /// <summary>A Saturday morning, so staging plans the week that starts the next day.</summary>
    private static readonly DateTimeOffset Saturday = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private static readonly ShoppingTargetDraft KitchenList =
        new(null, "Kitchen list", ShoppingTargetKind.HomeAssistant, "http://homeassistant.local:8123/", "todo.shopping_list", "secret");

    private static SaveShoppingTargetsRequest Targets(params ShoppingTargetDraft[] targets) => new(targets);

    private static TestCore WithLibrary()
    {
        var core = new TestCore();
        core.Clock.Now = Saturday;
        core.Recipes.Rows.AddRange(TestData.Library);
        return core;
    }

    [Test]
    public async Task Saved_targets_never_give_their_token_back()
    {
        var core = new TestCore();

        var saved = (await core.PlanManager().SaveShoppingTargetsAsync(Targets(KitchenList), default)).Single();

        await Assert.That(saved.Name).IsEqualTo("Kitchen list");
        await Assert.That(saved.HasToken).IsTrue();
        await Assert.That(core.Settings.ShoppingTargets.Single().Token).IsEqualTo("secret");
        await Assert.That((await core.PlanManager().GetShoppingTargetsAsync(default)).Single()).IsEqualTo(saved);
    }

    [Test]
    public async Task Editing_a_target_without_a_token_keeps_the_saved_one()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        var saved = (await plans.SaveShoppingTargetsAsync(Targets(KitchenList), default)).Single();

        var renamed = (await plans.SaveShoppingTargetsAsync(
            Targets(KitchenList with { Id = saved.Id, Name = "Fridge list", Token = null }), default)).Single();

        await Assert.That(renamed.Id).IsEqualTo(saved.Id);
        await Assert.That(renamed.Name).IsEqualTo("Fridge list");
        await Assert.That(core.Settings.ShoppingTargets.Single().Token).IsEqualTo("secret");
    }

    [Test]
    public async Task Each_kind_must_have_what_it_needs_to_be_reached()
    {
        var plans = new TestCore().PlanManager();
        async Task<string?> Problem(ShoppingTargetDraft draft) =>
            (await TestData.Caught<RuleViolationException>(() => plans.SaveShoppingTargetsAsync(Targets(draft), default)))?.Message;

        await Assert.That(await Problem(KitchenList with { Token = null })).Contains("long-lived access token");
        await Assert.That(await Problem(KitchenList with { List = "shopping_list" })).Contains("todo.shopping_list");
        await Assert.That(await Problem(KitchenList with { Url = "homeassistant.local" })).Contains("http://homeassistant.local:8123");
        await Assert.That(await Problem(new(null, "Todoist", ShoppingTargetKind.Todoist))).Contains("Todoist's settings");
        await Assert.That(await Problem(new(null, "Automation", ShoppingTargetKind.Webhook))).Contains("webhook address");
        await Assert.That(await Problem(KitchenList with { Name = " " })).Contains("needs a name");
        await Assert.That(await TestData.Caught<RuleViolationException>(() =>
            plans.SaveShoppingTargetsAsync(Targets(KitchenList, KitchenList with { Name = "kitchen LIST" }), default))).IsNotNull();
    }

    [Test]
    public async Task Todoist_keeps_no_address_and_a_webhook_no_list()
    {
        var plans = new TestCore().PlanManager();

        var saved = await plans.SaveShoppingTargetsAsync(Targets(
            new(null, "Todoist", ShoppingTargetKind.Todoist, "https://ignored.example", "2203306141", "token"),
            new(null, "Automation", ShoppingTargetKind.Webhook, "https://hooks.example/list", "ignored")), default);

        await Assert.That(saved[0].Url).IsEqualTo(string.Empty);
        await Assert.That(saved[0].List).IsEqualTo("2203306141");
        await Assert.That(saved[1].List).IsEqualTo(string.Empty);
        await Assert.That(saved[1].HasToken).IsFalse();
    }

    [Test]
    public async Task Sending_adds_what_the_plan_still_needs_and_the_hand_added_items()
    {
        var core = WithLibrary();
        var plans = core.PlanManager();
        var target = (await plans.SaveShoppingTargetsAsync(Targets(KitchenList), default)).Single();
        var plan = await plans.StageAsync(new StagePlanRequest(), default);
        await plans.AddManualItemAsync(new NewShoppingItem("dish soap", 1, "bottle"), default);
        var toBuy = (await plans.GetIngredientsAsync(plan.Id, default)).Ingredients.Count(line => line.Status == IngredientStatus.ToBuy);

        var result = await plans.SendShoppingListAsync(new SendShoppingListRequest(target.Id, plan.Id), default);

        await Assert.That(result.Total).IsEqualTo(toBuy + 1);
        await Assert.That(result.Sent).IsEqualTo(result.Total);
        await Assert.That(result.Problem).IsNull();
        await Assert.That(core.ToDoLists.Sent.Select(sent => sent.Item.Title)).Contains("dish soap, 1 bottle");
        await Assert.That(core.ToDoLists.Sent.All(sent => sent.Target.Token == "secret")).IsTrue();
    }

    [Test]
    public async Task Without_a_plan_only_the_hand_added_items_go()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        var target = (await plans.SaveShoppingTargetsAsync(Targets(KitchenList), default)).Single();
        await plans.AddManualItemAsync(new NewShoppingItem("milk", 2, "l"), default);

        var result = await plans.SendShoppingListAsync(new SendShoppingListRequest(target.Id), default);

        await Assert.That(result.Total).IsEqualTo(1);
        await Assert.That(core.ToDoLists.Sent.Single().Item.Title).IsEqualTo("milk, 2 l");
    }

    [Test]
    public async Task An_empty_list_or_a_missing_target_sends_nothing()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        var target = (await plans.SaveShoppingTargetsAsync(Targets(KitchenList), default)).Single();

        var empty = await TestData.Caught<RuleViolationException>(() => plans.SendShoppingListAsync(new SendShoppingListRequest(target.Id), default));
        var missing = await TestData.Caught<NotFoundException>(() => plans.SendShoppingListAsync(new SendShoppingListRequest(Guid.NewGuid()), default));

        await Assert.That(empty?.Message).IsEqualTo("Nothing is left to buy, so nothing was sent.");
        await Assert.That(missing).IsNotNull();
        await Assert.That(core.ToDoLists.Sent).IsEmpty();
    }

    [Test]
    public async Task A_refused_item_stops_the_rest_and_says_how_far_it_got()
    {
        var core = new TestCore();
        var plans = core.PlanManager();
        var target = (await plans.SaveShoppingTargetsAsync(Targets(KitchenList), default)).Single();
        await plans.AddManualItemAsync(new NewShoppingItem("milk"), default);
        await plans.AddManualItemAsync(new NewShoppingItem("eggs"), default);
        core.ToDoLists.TakeOnly = 1;

        var result = await plans.SendShoppingListAsync(new SendShoppingListRequest(target.Id), default);

        await Assert.That(result.Sent).IsEqualTo(1);
        await Assert.That(result.Total).IsEqualTo(2);
        await Assert.That(result.Problem).IsEqualTo("Kitchen list answered HTTP 500.");
    }

    [Test]
    public async Task Items_read_as_an_amount_with_their_store_and_meals()
    {
        var items = ShoppingEngine.ToDoItems([
            new OverviewLine("limes", IngredientStatus.ToBuy, 3, string.Empty, null, IngredientGroup.Fruit, null, false, [], null, StoreArea.Produce, [], null),
            new OverviewLine("fish fillets", IngredientStatus.ToBuy, 600, "g", "Corner Market", IngredientGroup.Protein, null, false, ["Slaw Fish Tacos"], null, StoreArea.MeatAndSeafood, [], null),
            new OverviewLine("coconut milk", IngredientStatus.ToBuy, null, "can", string.Empty, IngredientGroup.Other, null, true, ["Curry", "Soup"], null, StoreArea.CannedAndDry, [], null),
        ]);

        await Assert.That(items.Select(item => item.Title)).IsEquivalentTo(["fish fillets, 600 g", "coconut milk", "limes, 3"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(items[0].Description).IsEqualTo("Corner Market · For Slaw Fish Tacos");
        await Assert.That(items[1].Description).IsEqualTo("For Curry, Soup · Optional");
        await Assert.That(items[2].Description).IsNull();
    }

    [Test]
    public async Task Items_list_every_amount_and_what_could_be_used_instead()
    {
        var items = ShoppingEngine.ToDoItems([
            new OverviewLine("shallot", IngredientStatus.ToBuy, 2, string.Empty, null, IngredientGroup.Herb, null, false, [], null, StoreArea.Produce,
                [new ShoppingAmount(3, "tbsp")], "onion"),
        ]);

        await Assert.That(items[0].Title).IsEqualTo("shallot, 2 + 3 tbsp");
        await Assert.That(items[0].Description).IsEqualTo("Or onion");
    }
}
