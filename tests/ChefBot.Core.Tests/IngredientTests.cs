using ChefBot.Core.Ingredients;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Tests;

public class IngredientTests
{
    [Test]
    public async Task Recognizes_aliases_and_plurals()
    {
        await Assert.That(TestData.Catalog.Find("Scallions, thinly sliced")?.Name).IsEqualTo("green onion");
        await Assert.That(TestData.Catalog.Find("3 carrots")?.Name).IsEqualTo("carrot");
        await Assert.That(TestData.Catalog.Find("diced tomatoes")?.Name).IsEqualTo("canned tomatoes");
    }

    [Test]
    public async Task Processed_food_is_not_the_fresh_ingredient()
    {
        await Assert.That(TestData.Catalog.Find("sun-dried tomatoes") is null).IsTrue();
        await Assert.That(TestData.Catalog.Find("tomato juice") is null).IsTrue();
        await Assert.That(TestData.Catalog.Find("chicken broth")?.Name).IsEqualTo("chicken broth");
        await Assert.That(TestData.Catalog.Find("beef stock") is null).IsTrue();
    }

    [Test]
    public async Task Proteins_keep_their_exact_name_but_match_the_catalog()
    {
        var thigh = TestData.Catalog.Identify("chicken thigh");

        await Assert.That(thigh.Key).IsEqualTo("chicken thigh");
        await Assert.That(thigh.CatalogName).IsEqualTo("chicken");
        await Assert.That(thigh.MatchName()).IsEqualTo("chicken");
    }

    [Test]
    public async Task Ordinary_ingredients_share_one_key()
    {
        var scallion = TestData.Catalog.Identify("scallions");

        await Assert.That(scallion.Key).IsEqualTo("green onion");
        await Assert.That(scallion.DisplayName).IsEqualTo("green onion");
    }

    [Test]
    public async Task Unknown_ingredients_are_kept_as_typed()
    {
        var saffron = TestData.Catalog.Identify("Saffron threads");

        await Assert.That(saffron.Key).IsEqualTo("saffron threads");
        await Assert.That(saffron.CatalogName is null).IsTrue();
        await Assert.That(saffron.DisplayName).IsEqualTo("Saffron threads");
    }

    [Test]
    public async Task Parses_amount_unit_and_name()
    {
        var rice = IngredientLineParser.Parse("1 1/2 cups brown rice");
        var chicken = IngredientLineParser.Parse("2 lbs of chicken thighs");
        var carrots = IngredientLineParser.Parse("3 carrots");

        await Assert.That(rice.Quantity == 1.5m).IsTrue();
        await Assert.That(rice.Unit).IsEqualTo("cup");
        await Assert.That(rice.Name).IsEqualTo("brown rice");
        await Assert.That(chicken.Quantity == 2m).IsTrue();
        await Assert.That(chicken.Unit).IsEqualTo("lb");
        await Assert.That(chicken.Name).IsEqualTo("chicken thighs");
        await Assert.That(carrots.Quantity == 3m).IsTrue();
        await Assert.That(carrots.Unit).IsEqualTo(string.Empty);
        await Assert.That(carrots.Name).IsEqualTo("carrots");
    }

    [Test]
    public async Task Lines_without_an_amount_are_all_name()
    {
        var salt = IngredientLineParser.Parse("salt to taste");

        await Assert.That(salt.Quantity is null).IsTrue();
        await Assert.That(salt.Name).IsEqualTo("salt to taste");
    }

    [Test]
    public async Task Classifies_recipes_by_whole_word_labels()
    {
        var classifier = new RecipeClassifier(new ClassifierOptions
        {
            BreakfastWords = ["breakfast"],
            SweetWords = ["dessert", "cake"],
            SlowCookerWords = ["slow cooker"],
            NotAMealWords = ["side", "sides"],
        });

        var potRoast = classifier.Classify("Pot Roast", ["Slow-Cooker", "Dinner"]);
        await Assert.That(potRoast.Course).IsEqualTo(RecipeCourse.Main);
        await Assert.That(potRoast.IsSlowCooker).IsTrue();
        await Assert.That(classifier.Classify("Lemon Bars", ["Dessert"]).Course).IsEqualTo(RecipeCourse.Sweet);
        await Assert.That(classifier.Classify("Pancakes", ["Breakfast"]).Course).IsEqualTo(RecipeCourse.Breakfast);
        await Assert.That(classifier.Classify("Coleslaw", ["Sides"]).Course).IsEqualTo(RecipeCourse.Other);

        // "cake" must not match inside "Pancakes", and names are not used to pick a course.
        await Assert.That(classifier.Classify("Crab Cakes", ["Pancakes night"]).Course).IsEqualTo(RecipeCourse.Main);
    }
}
