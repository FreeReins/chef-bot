using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Engines;

namespace ChefBot.Core.Tests;

public class PantryAndSwapTests
{
    [Test]
    public async Task Expiration_is_estimated_from_shelf_life_unless_given()
    {
        var estimated = TestData.Stocked("cabbage");
        var labelled = TestData.Stocked("cabbage", expiresOn: TestData.Sunday.AddDays(3));
        var unknown = TestData.Stocked("saffron");

        await Assert.That(estimated.ExpiresOn == TestData.Sunday.AddDays(14)).IsTrue();
        await Assert.That(labelled.ExpiresOn == TestData.Sunday.AddDays(3)).IsTrue();
        await Assert.That(unknown.ExpiresOn is null).IsTrue();
    }

    [Test]
    public async Task Freshness_follows_the_expiration_date()
    {
        var item = TestData.Stocked("kale", expiresOn: TestData.Sunday.AddDays(5));

        await Assert.That(item.FreshnessOn(TestData.Sunday, 3)).IsEqualTo(Freshness.Fresh);
        await Assert.That(item.FreshnessOn(TestData.Sunday.AddDays(3), 3)).IsEqualTo(Freshness.ExpiringSoon);
        await Assert.That(item.FreshnessOn(TestData.Sunday.AddDays(6), 3)).IsEqualTo(Freshness.Expired);
        await Assert.That(TestData.Stocked("saffron").FreshnessOn(TestData.Sunday, 3)).IsEqualTo(Freshness.Unknown);
    }

    [Test]
    public async Task Cooking_uses_up_pantry_stock_oldest_first()
    {
        var older = TestData.Stocked("cabbage", 0.25m, "head", TestData.Sunday.AddDays(2));
        var newer = TestData.Stocked("cabbage", 1m, "head", TestData.Sunday.AddDays(10));
        var tacos = TestData.Meal(TestData.Tacos, MealSlot.Dinner, TestData.Sunday, portions: 4);

        var finished = TestData.Shopping.DeductCooked([newer, older], tacos);

        // The recipe needs half a head: all of the older quarter, then a quarter of the newer one.
        await Assert.That(finished.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(finished[0], older)).IsTrue();
        await Assert.That(newer.Quantity == 0.75m).IsTrue();
    }

    [Test]
    public async Task Swap_suggestions_favor_recipes_that_use_expiring_food()
    {
        var pantry = TestData.PantryOf(
            TestData.Stocked("cabbage", 1m, "head", TestData.Sunday.AddDays(1)),
            TestData.Stocked("cilantro", 1m, "bunch"));

        var ranked = TestData.Suggestions.Rank([TestData.Caprese, TestData.Soup, TestData.Tacos], pantry, month: 11, limit: 2);

        await Assert.That(ranked.Count).IsEqualTo(2);
        await Assert.That(ranked[0].Recipe.Name).IsEqualTo(TestData.Tacos.Name);
        await Assert.That(ranked[0].ExpiringSoon.Contains("cabbage")).IsTrue();
        await Assert.That(ranked[0].OnHand.Contains("cilantro")).IsTrue();
        await Assert.That(ranked[0].ToBuy.Contains("cod")).IsTrue();
        await Assert.That(ranked[0].ToBuy.Contains("cabbage")).IsFalse();
    }

    [Test]
    public async Task Creative_meals_combine_a_protein_produce_and_a_whole_grain_from_the_pantry()
    {
        var pantry = TestData.PantryOf(
            TestData.Stocked("brown rice", 4m, "cup"),
            TestData.Stocked("chicken thigh", 1m, "lb"),
            TestData.Stocked("cabbage", 1m, "head"),
            TestData.Stocked("kale", 1m, "bunch"),
            TestData.Stocked("cilantro", 1m, "bunch"));

        var meals = TestData.Suggestions.Ideas(pantry, count: 5, new Random(7));

        // One grain and one protein with two vegetables gives exactly two different plates.
        await Assert.That(meals.Count).IsEqualTo(2);
        foreach (var meal in meals)
        {
            await Assert.That(meal.Parts.Count).IsEqualTo(3);
            await Assert.That(meal.Parts[0].Group).IsEqualTo(IngredientGroup.Protein);
            await Assert.That(meal.Parts[1].Group).IsEqualTo(IngredientGroup.Vegetable);
            await Assert.That(meal.Parts[2].Name).IsEqualTo("brown rice");
            await Assert.That(meal.Parts.All(part => part.OnHand)).IsTrue();
            await Assert.That(meal.Title.Contains("brown rice", StringComparison.OrdinalIgnoreCase)).IsTrue();
        }
    }

    [Test]
    public async Task Creative_meals_still_work_with_an_empty_pantry()
    {
        var meals = TestData.Suggestions.Ideas(PantrySnapshot.Empty, count: 2, new Random(1));

        await Assert.That(meals.Count).IsEqualTo(2);
        await Assert.That(meals.SelectMany(meal => meal.Parts).Any(part => part.OnHand)).IsFalse();
    }
}
