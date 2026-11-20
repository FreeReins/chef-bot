using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class RecipeFileTests
{
    private static readonly TransformationEngine Reader = TestData.Transformation;

    private static string? ErrorFrom(Action read)
    {
        try
        {
            read();
            return null;
        }
        catch (RuleViolationException error)
        {
            return error.Message;
        }
    }

    [Test]
    public async Task Reads_chefbots_own_shape()
    {
        var file = Reader.ReadFile("dinners.json", """
            [{ "name": "Chili", "course": "main", "isSlowCooker": true, "servings": 6,
               "ingredients": ["1 lb ground beef", "1 can beans"] }]
            """);

        var chili = file.Recipes.Single();
        await Assert.That(chili.Name).IsEqualTo("Chili");
        await Assert.That(chili.Course).IsEqualTo(RecipeCourse.Main);
        await Assert.That(chili.IsSlowCooker).IsTrue();
        await Assert.That(chili.Servings).IsEqualTo(6m);
        await Assert.That(chili.Ingredients.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Reads_a_schema_org_recipe_from_a_json_ld_graph()
    {
        var file = Reader.ReadFile("Pancakes.JSON", """
            { "@context": "https://schema.org", "@graph": [
                { "@type": "WebPage", "name": "Not a recipe" },
                { "@type": ["Recipe"], "name": "Pancakes", "recipeYield": "4 servings",
                  "recipeCategory": "Breakfast", "recipeIngredient": ["2 cups flour", "2 eggs"] } ] }
            """);

        var pancakes = file.Recipes.Single();
        await Assert.That(pancakes.Course).IsEqualTo(RecipeCourse.Breakfast);
        await Assert.That(pancakes.Servings).IsEqualTo(4m);
        await Assert.That(file.Skipped).IsEqualTo(0);
    }

    [Test]
    public async Task Reads_mealie_ingredient_objects_and_tags()
    {
        var file = Reader.ReadFile("mealie.json", """
            { "name": "Pot Roast", "recipeServings": 8,
              "tags": [{ "name": "Slow Cooker" }],
              "recipeIngredient": [{ "display": "3 lb chuck roast" }, { "note": "4 carrots" }, { "note": "" }] }
            """);

        var roast = file.Recipes.Single();
        await Assert.That(roast.IsSlowCooker).IsTrue();
        await Assert.That(roast.Servings).IsEqualTo(8m);
        await Assert.That(roast.Ingredients).IsEquivalentTo(["3 lb chuck roast", "4 carrots"]);
    }

    [Test]
    public async Task Counts_entries_without_a_name_or_ingredients_as_skipped()
    {
        var file = Reader.ReadFile("mixed.json", """
            { "recipes": [
                { "name": "Toast", "ingredients": ["2 slices bread"] },
                { "name": "Nothing in it", "ingredients": [] },
                { "ingredients": ["1 apple"] } ] }
            """);

        await Assert.That(file.Recipes.Count).IsEqualTo(1);
        await Assert.That(file.Skipped).IsEqualTo(2);
    }

    [Test]
    public async Task Explains_files_it_cannot_read()
    {
        await Assert.That(ErrorFrom(() => Reader.ReadFile("recipes.json", "not json"))).Contains("isn't a JSON recipe file");
        await Assert.That(ErrorFrom(() => Reader.ReadFile("recipes.json", "[]"))).Contains("No recipes were found");
        await Assert.That(ErrorFrom(() => Reader.ReadFile("recipes.csv", "name"))).Contains("can't import .csv files yet");
    }

    private const string MealieBackup = """
        {
          "alembic_version": [{ "version_num": "abc" }],
          "recipes": [
            { "id": "r1", "name": "Slow Cooker Chili", "recipe_servings": 0.0, "recipe_yield": "6 bowls", "recipe_yield_quantity": 0.0 },
            { "id": "r2", "name": "Lemon Bundt", "recipe_servings": 12.0 },
            { "id": "r3", "name": "Empty draft", "recipe_servings": 4.0 }
          ],
          "recipes_ingredients": [
            { "recipe_id": "r1", "position": 1, "note": "1 can beans", "food_id": null, "unit_id": null, "quantity": 0.0 },
            { "recipe_id": "r1", "position": 0, "note": "", "food_id": "f1", "unit_id": "u1", "quantity": 1.5 },
            { "recipe_id": "r1", "position": 2, "title": "For the topping", "note": null, "food_id": null },
            { "recipe_id": "r2", "position": 0, "original_text": "2 lemons, zested", "note": "2 lemons" }
          ],
          "ingredient_foods": [{ "id": "f1", "name": "ground beef" }],
          "ingredient_units": [{ "id": "u1", "name": "pound", "abbreviation": "lb", "use_abbreviation": true }],
          "tags": [{ "id": "t1", "name": "Slow Cooker" }],
          "recipes_to_tags": [{ "recipe_id": "r1", "tag_id": "t1" }],
          "categories": [{ "id": "c1", "name": "Dessert" }],
          "recipes_to_categories": [{ "recipe_id": "r2", "category_id": "c1" }]
        }
        """;

    [Test]
    public async Task Reads_a_mealie_backup_database()
    {
        var file = Reader.ReadFile("database.json", MealieBackup);

        await Assert.That(file.Recipes.Count).IsEqualTo(2);
        await Assert.That(file.Skipped).IsEqualTo(1);

        var chili = file.Recipes.Single(recipe => recipe.Name == "Slow Cooker Chili");
        await Assert.That(chili.Ingredients).IsEquivalentTo(["1.5 lb ground beef", "1 can beans"]);
        await Assert.That(chili.Ingredients[0]).IsEqualTo("1.5 lb ground beef");
        await Assert.That(chili.IsSlowCooker).IsTrue();
        await Assert.That(chili.Servings).IsEqualTo(6m);

        var bundt = file.Recipes.Single(recipe => recipe.Name == "Lemon Bundt");
        await Assert.That(bundt.Course).IsEqualTo(RecipeCourse.Sweet);
        await Assert.That(bundt.Ingredients).IsEquivalentTo(["2 lemons, zested"]);
        await Assert.That(bundt.Servings).IsEqualTo(12m);
    }
}
