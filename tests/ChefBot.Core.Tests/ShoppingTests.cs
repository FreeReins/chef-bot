using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Shopping;

namespace ChefBot.Core.Tests;

public class ShoppingTests
{
    /// <summary>Three dinners sized to cook each recipe exactly once.</summary>
    private static MealPlan PlanOf(params Recipes.Recipe[] recipes)
    {
        var plan = new MealPlan { WeekStart = TestData.Sunday };
        for (var index = 0; index < recipes.Length; index++)
        {
            plan.Meals.Add(PlannedMeal.ForRecipe(recipes[index], MealSlot.Dinner, TestData.Sunday.AddDays(index * 2), portions: 4));
        }

        return plan;
    }

    private static ShoppingItem? Find(ShoppingDraft draft, string key) => draft.Items.FirstOrDefault(item => item.Key == key);

    [Test]
    public async Task Totals_an_ingredient_shared_by_two_meals()
    {
        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Tacos, TestData.Bowls), PantrySnapshot.Empty);
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

        var draft = TestData.ShoppingBuilder().Build(plan, PantrySnapshot.Empty);

        await Assert.That(Find(draft, "cabbage")?.Quantity == 0.75m).IsTrue();
        await Assert.That(Find(draft, "tortilla")?.Quantity == 12m).IsTrue();
    }

    [Test]
    public async Task Leaves_staples_off_the_list()
    {
        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Stew), PantrySnapshot.Empty);

        await Assert.That(Find(draft, "salt") is null).IsTrue();
        await Assert.That(draft.AssumedStaples.Contains("salt")).IsTrue();
    }

    [Test]
    public async Task Keeps_the_exact_cut_of_meat_on_the_list()
    {
        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Tacos, TestData.Bowls), PantrySnapshot.Empty);

        await Assert.That(Find(draft, "cod")?.DisplayName).IsEqualTo("cod");
        await Assert.That(Find(draft, "chicken thigh")?.DisplayName).IsEqualTo("chicken thigh");
    }

    [Test]
    public async Task Buys_only_what_the_pantry_is_short_of()
    {
        var pantry = TestData.PantryOf(TestData.Stocked("cabbage", 0.5m, "head"));
        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Tacos, TestData.Bowls), pantry);

        await Assert.That(Find(draft, "cabbage")?.Quantity == 0.5m).IsTrue();
    }

    [Test]
    public async Task Enough_in_the_pantry_means_nothing_to_buy()
    {
        var pantry = TestData.PantryOf(TestData.Stocked("cabbage", 2m, "head"));
        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Tacos, TestData.Bowls), pantry);

        await Assert.That(Find(draft, "cabbage") is null).IsTrue();
        await Assert.That(draft.CoveredByPantry.Contains("cabbage")).IsTrue();
    }

    [Test]
    public async Task An_untracked_amount_in_the_pantry_counts_as_enough()
    {
        var pantry = TestData.PantryOf(TestData.Stocked("cabbage"));
        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Tacos), pantry);

        await Assert.That(Find(draft, "cabbage") is null).IsTrue();
    }

    [Test]
    public async Task Expired_food_does_not_count()
    {
        var expired = TestData.Stocked("cabbage", 2m, "head");
        expired.ExpiresOn = TestData.Sunday.AddDays(-1);

        var draft = TestData.ShoppingBuilder().Build(PlanOf(TestData.Tacos, TestData.Bowls), TestData.PantryOf(expired));

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

        var draft = TestData.ShoppingBuilder().Build(plan, PantrySnapshot.Empty);

        await Assert.That(draft.Items.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Groceries_only_optional_meals_need_are_marked_optional()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var draft = TestData.ShoppingBuilder().Build(plan, PantrySnapshot.Empty);

        // Oats are only used by the optional oatmeal breakfast.
        await Assert.That(Find(draft, "oats")?.IsOptional == true).IsTrue();
        await Assert.That(Find(draft, "cabbage")?.IsOptional == false).IsTrue();
    }

    [Test]
    public async Task Rebuilding_the_list_keeps_what_was_already_bought()
    {
        var plan = PlanOf(TestData.Tacos);
        plan.ReplaceOpenShoppingItems(TestData.ShoppingBuilder().Build(plan, PantrySnapshot.Empty).Items);
        var cod = plan.ShoppingItems.Single(item => item.Key == "cod");
        cod.IsPurchased = true;

        // The cod is now in the pantry, so the rebuilt list no longer asks for it.
        var pantry = TestData.PantryOf(TestData.Stocked("cod", 1m, "lb"));
        plan.ReplaceOpenShoppingItems(TestData.ShoppingBuilder().Build(plan, pantry).Items);

        await Assert.That(plan.ShoppingItems.Count(item => item.Key == "cod")).IsEqualTo(1);
        await Assert.That(plan.ShoppingItems.Single(item => item.Key == "cod").IsPurchased).IsTrue();
    }

    [Test]
    public async Task The_ingredients_view_says_where_each_ingredient_comes_from()
    {
        var plan = PlanOf(TestData.Tacos, TestData.Stew);
        var cabbage = TestData.Stocked("cabbage", 1m, "head");
        var apples = TestData.Stocked("apples", 4m);
        plan.ReplaceOpenShoppingItems(TestData.ShoppingBuilder().Build(plan, TestData.PantryOf(cabbage, apples)).Items);

        var overview = new IngredientOverviewBuilder(TestData.Catalog, TestData.Options)
            .Build(plan, [cabbage, apples], TestData.Sunday);

        IngredientStatus StatusOf(string name) => overview.Ingredients.First(line => line.Name == name).Status;

        await Assert.That(StatusOf("cabbage")).IsEqualTo(IngredientStatus.InPantry);
        await Assert.That(StatusOf("cod")).IsEqualTo(IngredientStatus.ToBuy);
        await Assert.That(StatusOf("salt")).IsEqualTo(IngredientStatus.Staple);
        await Assert.That(overview.Ingredients.First(line => line.Name == "cabbage").UsedIn.Contains(TestData.Tacos.Name)).IsTrue();

        // The pantry list flags what the plan won't touch, which is what a swap could use up.
        await Assert.That(overview.Pantry.Single(line => line.Item.Key == "cabbage").UsedByPlan).IsTrue();
        await Assert.That(overview.Pantry.Single(line => line.Item.Key == "apple").UsedByPlan).IsFalse();
    }
}
