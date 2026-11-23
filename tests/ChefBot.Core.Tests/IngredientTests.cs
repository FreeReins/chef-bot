using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

public class IngredientTests
{
    [Test]
    public async Task Every_ingredient_in_the_catalog_has_a_part_of_the_store()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "config", "ingredients.json");
        var catalog = IngredientCatalog.FromJson(File.ReadAllText(file));

        var unplaced = catalog.Entries.Where(entry => entry.Area == StoreArea.Other).Select(entry => entry.Name).ToList();

        await Assert.That(catalog.Entries.Count).IsGreaterThan(100);
        await Assert.That(unplaced).IsEmpty();
        await Assert.That(catalog.Get("canned tomatoes")?.Area).IsEqualTo(StoreArea.CannedAndDry);
        await Assert.That(catalog.Get("carrot")?.Area).IsEqualTo(StoreArea.Produce);
    }

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
    public async Task Proteins_keep_their_exact_cut_but_match_the_catalog()
    {
        var thigh = TestData.Catalog.Identify("chicken thigh");

        await Assert.That(thigh.Key).IsEqualTo("chicken thigh");
        await Assert.That(thigh.CatalogName).IsEqualTo("chicken");
        await Assert.That(thigh.MatchName()).IsEqualTo("chicken");
    }

    [Test]
    public async Task Different_wordings_of_one_cut_share_a_key()
    {
        var keys = new[] { "chicken breasts", "boneless skinless chicken breasts", "large chicken breast" }
            .Select(text => TestData.Catalog.Identify(text))
            .ToList();

        await Assert.That(keys.Select(identity => identity.Key).Distinct()).IsEquivalentTo(["chicken breast"]);
        await Assert.That(keys[1].DisplayName).IsEqualTo("chicken breast");
        await Assert.That(TestData.Catalog.Identify("cod fillets").Key).IsEqualTo("cod");
        await Assert.That(TestData.Catalog.Identify("chicken").Key).IsEqualTo("chicken");
    }

    [Test]
    public async Task Specific_names_prefer_the_alias_over_the_general_name()
    {
        var squash = TestData.Catalog.Identify("1 butternut squash");

        await Assert.That(squash.Key).IsEqualTo("butternut squash");
        await Assert.That(squash.CatalogName).IsEqualTo("winter squash");
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
    [Arguments("2 ½ cups half and half", 2.5, "cup", "half and half")]
    [Arguments("1½ lb chicken breast", 1.5, "lb", "chicken breast")]
    [Arguments("1⁄2 pound ground beef", 0.5, "lb", "ground beef")]
    [Arguments("▢ 1/2 pound bacon", 0.5, "lb", "bacon")]
    [Arguments("¾ tsp. salt", 0.75, "tsp", "salt")]
    [Arguments("2-4 chicken breasts, shredded", 4, "", "chicken breasts")]
    [Arguments("1 to 2 cups milk", 2, "cup", "milk")]
    [Arguments("1 (14 oz) can diced tomatoes", 1, "can", "diced tomatoes")]
    [Arguments("2 lbs chicken breasts , (boneless, skinless)", 2, "lb", "chicken breasts")]
    [Arguments("1 fl oz vanilla", 1, "fl oz", "vanilla")]
    [Arguments("2 T olive oil", 2, "tbsp", "olive oil")]
    public async Task Reads_fractions_ranges_and_prep_notes(string line, double quantity, string unit, string name)
    {
        var parsed = IngredientLineParser.Parse(line);

        await Assert.That(parsed.Quantity).IsEqualTo((decimal)quantity);
        await Assert.That(parsed.Unit).IsEqualTo(unit);
        await Assert.That(parsed.Name).IsEqualTo(name);
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
        var words = new ClassifierWords
        {
            BreakfastWords = ["breakfast"],
            SnackWords = [],
            SweetWords = ["dessert", "cake"],
            DrinkWords = [],
            SlowCookerWords = ["slow cooker"],
            NotAMealWords = ["side", "sides"],
        };

        var potRoast = RecipeClassifier.Classify("Pot Roast", ["Slow-Cooker", "Dinner"], words);
        await Assert.That(potRoast.Course).IsEqualTo(RecipeCourse.Main);
        await Assert.That(potRoast.IsSlowCooker).IsTrue();
        await Assert.That(RecipeClassifier.Classify("Lemon Bars", ["Dessert"], words).Course).IsEqualTo(RecipeCourse.Sweet);
        await Assert.That(RecipeClassifier.Classify("Pancakes", ["Breakfast"], words).Course).IsEqualTo(RecipeCourse.Breakfast);
        await Assert.That(RecipeClassifier.Classify("Coleslaw", ["Sides"], words).Course).IsEqualTo(RecipeCourse.Other);

        // "cake" must not match inside "Pancakes", and names are not used to pick a course.
        await Assert.That(RecipeClassifier.Classify("Crab Cakes", ["Pancakes night"], words).Course).IsEqualTo(RecipeCourse.Main);
    }
}
