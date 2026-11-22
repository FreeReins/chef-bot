using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class ShoppingTests
{
    /// <summary>Three dinners sized to cook each recipe exactly once.</summary>
    private static MealPlan PlanOf(params Recipe[] recipes)
    {
        var plan = new MealPlan { WeekStart = TestData.Sunday };
        for (var index = 0; index < recipes.Length; index++)
        {
            plan.Meals.Add(TestData.Meal(recipes[index], MealSlot.Dinner, TestData.Sunday.AddDays(index * 2), portions: 4));
        }

        return plan;
    }

    private static ShoppingItem? Find(ShoppingDraft draft, string key) => draft.Items.FirstOrDefault(item => item.Key == key);

    [Test]
    public async Task Totals_an_ingredient_shared_by_two_meals()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos, TestData.Bowls), PantrySnapshot.Empty, []);
        var cabbage = Find(draft, "cabbage");
        var cilantro = Find(draft, "cilantro");

        await Assert.That(cabbage?.Quantity == 1m).IsTrue();
        await Assert.That(cabbage?.Unit).IsEqualTo("head");
        await Assert.That(cilantro?.Quantity == 1.5m).IsTrue();
        await Assert.That(draft.Items.Count(item => item.Key == "cabbage")).IsEqualTo(1);
    }

    [Test]
    public async Task Scales_to_the_portions_being_cooked()
    {
        var plan = PlanOf(TestData.Tacos);
        plan.Meals[0].Portions = 6; // one and a half batches of a recipe that serves four

        var draft = TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []);

        await Assert.That(Find(draft, "cabbage")?.Quantity == 0.75m).IsTrue();
        await Assert.That(Find(draft, "tortilla")?.Quantity == 12m).IsTrue();
    }

    [Test]
    public async Task Leaves_staples_off_the_list()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Stew), PantrySnapshot.Empty, []);

        await Assert.That(Find(draft, "salt") is null).IsTrue();
        await Assert.That(draft.AssumedStaples.Contains("salt")).IsTrue();
    }

    [Test]
    public async Task Keeps_the_exact_cut_of_meat_on_the_list()
    {
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos, TestData.Bowls), PantrySnapshot.Empty, []);

        await Assert.That(Find(draft, "cod")?.DisplayName).IsEqualTo("cod");
        await Assert.That(Find(draft, "chicken thigh")?.DisplayName).IsEqualTo("chicken thigh");
    }

    [Test]
    public async Task Buys_only_what_the_pantry_is_short_of()
    {
        var pantry = TestData.PantryOf(TestData.Stocked("cabbage", 0.5m, "head"));
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos, TestData.Bowls), pantry, []);

        await Assert.That(Find(draft, "cabbage")?.Quantity == 0.5m).IsTrue();
    }

    [Test]
    public async Task Enough_in_the_pantry_means_nothing_to_buy()
    {
        var pantry = TestData.PantryOf(TestData.Stocked("cabbage", 2m, "head"));
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos, TestData.Bowls), pantry, []);

        await Assert.That(Find(draft, "cabbage") is null).IsTrue();
        await Assert.That(draft.CoveredByPantry.Contains("cabbage")).IsTrue();
    }

    [Test]
    public async Task An_untracked_amount_in_the_pantry_counts_as_enough()
    {
        var pantry = TestData.PantryOf(TestData.Stocked("cabbage"));
        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos), pantry, []);

        await Assert.That(Find(draft, "cabbage") is null).IsTrue();
    }

    [Test]
    public async Task Expired_food_does_not_count()
    {
        var expired = TestData.Stocked("cabbage", 2m, "head");
        expired.ExpiresOn = TestData.Sunday.AddDays(-1);

        var draft = TestData.Shopping.BuildList(PlanOf(TestData.Tacos, TestData.Bowls), TestData.PantryOf(expired), []);

        await Assert.That(Find(draft, "cabbage")?.Quantity == 1m).IsTrue();
    }

    [Test]
    public async Task Cooked_meals_and_leftovers_need_no_groceries()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        foreach (var meal in plan.Meals.Where(meal => !meal.IsLeftover))
        {
            meal.Status = MealStatus.Cooked;
        }

        var draft = TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []);

        await Assert.That(draft.Items.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Groceries_only_optional_meals_need_are_marked_optional()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var draft = TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []);

        // Oats are only used by the optional oatmeal breakfast.
        await Assert.That(Find(draft, "oats")?.IsOptional == true).IsTrue();
        await Assert.That(Find(draft, "cabbage")?.IsOptional == false).IsTrue();
    }

    [Test]
    public async Task Rebuilding_the_list_keeps_what_was_already_bought()
    {
        var plan = PlanOf(TestData.Tacos);
        TestData.Shopping.ApplyList(plan, TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []));
        var cod = plan.ShoppingItems.Single(item => item.Key == "cod");
        cod.IsPurchased = true;

        // The cod is now in the pantry, so the rebuilt list no longer asks for it.
        var pantry = TestData.PantryOf(TestData.Stocked("cod", 1m, "lb"));
        TestData.Shopping.ApplyList(plan, TestData.Shopping.BuildList(plan, pantry, []));

        await Assert.That(plan.ShoppingItems.Count(item => item.Key == "cod")).IsEqualTo(1);
        await Assert.That(plan.ShoppingItems.Single(item => item.Key == "cod").IsPurchased).IsTrue();
    }

    [Test]
    public async Task The_ingredients_view_says_where_each_ingredient_comes_from()
    {
        var plan = PlanOf(TestData.Tacos, TestData.Stew);
        var cabbage = TestData.Stocked("cabbage", 1m, "head");
        var apples = TestData.Stocked("apples", 4m);
        TestData.Shopping.ApplyList(plan, TestData.Shopping.BuildList(plan, TestData.PantryOf(cabbage, apples), []));

        var overview = TestData.Shopping.BuildOverview(plan, [cabbage, apples], TestData.Sunday, AdvancedSettings.DefaultExpiringSoonDays);

        IngredientStatus StatusOf(string name) => overview.Ingredients.First(line => line.Name == name).Status;

        await Assert.That(StatusOf("cabbage")).IsEqualTo(IngredientStatus.InPantry);
        await Assert.That(StatusOf("cod")).IsEqualTo(IngredientStatus.ToBuy);
        await Assert.That(StatusOf("salt")).IsEqualTo(IngredientStatus.Staple);
        await Assert.That(overview.Ingredients.First(line => line.Name == "cabbage").UsedIn.Contains(TestData.Tacos.Name)).IsTrue();

        // The pantry list flags what the plan won't touch, which is what a swap could use up.
        await Assert.That(overview.Pantry.Single(line => line.Item.Key == "cabbage").UsedByPlan).IsTrue();
        await Assert.That(overview.Pantry.Single(line => line.Item.Key == "apple").UsedByPlan).IsFalse();
    }

    [Test]
    public async Task The_pantry_view_lists_the_meals_that_use_each_item_soonest_first()
    {
        var plan = PlanOf(TestData.Bowls, TestData.Tacos, TestData.Bowls);
        var cabbage = TestData.Stocked("cabbage", 1m, "head");

        var line = TestData.Shopping.BuildOverview(plan, [cabbage], TestData.Sunday, AdvancedSettings.DefaultExpiringSoonDays).Pantry.Single();

        // Bowls are cooked once for two dinners, but both dinners are listed.
        await Assert.That(line.UsedFor.Select(meal => meal.Id)).IsEquivalentTo(plan.Meals.Select(meal => meal.Id), TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task The_pantry_view_leaves_out_meals_already_cooked_and_leftovers()
    {
        var plan = PlanOf(TestData.Tacos, TestData.Bowls);
        var dinners = TestData.Dinners(plan);
        dinners[0].Status = MealStatus.Cooked;
        plan.Meals.Add(new PlannedMeal
        {
            Slot = MealSlot.Lunch, Date = TestData.Sunday.AddDays(3), Title = "Leftovers", LeftoverOfMealId = dinners[1].Id,
        });
        var cabbage = TestData.Stocked("cabbage", 1m, "head");
        var apples = TestData.Stocked("apples", 4m);

        var pantry = TestData.Shopping.BuildOverview(plan, [cabbage, apples], TestData.Sunday, AdvancedSettings.DefaultExpiringSoonDays).Pantry;

        await Assert.That(pantry.Single(line => line.Item.Key == "cabbage").UsedFor.Select(meal => meal.Id)).IsEquivalentTo([dinners[1].Id]);
        await Assert.That(pantry.Single(line => line.Item.Key == "apple").UsedFor).IsEmpty();
    }

    [Test]
    public async Task Adds_up_amounts_written_in_different_units()
    {
        var dressing = TestData.NewRecipe("Dressing", RecipeCourse.Main, false, "2 tbsp tomato paste", "½ cup tomato paste");
        var draft = TestData.Shopping.BuildList(PlanOf(dressing), PantrySnapshot.Empty, []);

        var lines = draft.Items.Where(item => item.Key == "tomato paste").ToList();
        await Assert.That(lines.Count).IsEqualTo(1);
        await Assert.That(lines[0].Quantity).IsEqualTo(0.63m);
        await Assert.That(lines[0].Unit).IsEqualTo("cup");
    }

    [Test]
    public async Task Pantry_stock_in_another_unit_counts()
    {
        var stew = TestData.NewRecipe("Stew", RecipeCourse.Main, false, "½ lb stew meat", "1 lb cod");
        var pantry = TestData.PantryOf(TestData.Stocked("stew meat", 8m, "oz"), TestData.Stocked("cod", 4m, "oz"));

        var draft = TestData.Shopping.BuildList(PlanOf(stew), pantry, []);

        await Assert.That(Find(draft, "stew meat") is null).IsTrue();
        await Assert.That(Find(draft, "cod")?.Quantity).IsEqualTo(12m);
        await Assert.That(Find(draft, "cod")?.Unit).IsEqualTo("oz");
    }

    [Test]
    public async Task Cooking_takes_converted_amounts_from_the_pantry()
    {
        var rice = TestData.NewRecipe("Rice", RecipeCourse.Main, false, "1 cup brown rice");
        var meal = TestData.Meal(rice, MealSlot.Dinner, TestData.Sunday, portions: 4);
        var stock = TestData.Stocked("brown rice", 500m, "ml");

        var finished = TestData.Shopping.DeductCooked([stock], meal);

        await Assert.That(finished).IsEmpty();
        await Assert.That(Math.Round(stock.Quantity!.Value)).IsEqualTo(263m);
    }
}
