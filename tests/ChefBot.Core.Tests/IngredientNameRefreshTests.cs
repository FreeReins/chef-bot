using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class IngredientNameRefreshTests
{
    [Test]
    public async Task A_recipe_saved_under_an_old_name_is_filed_under_the_catalogs_current_one()
    {
        var core = new TestCore();
        var recipe = TestData.NewRecipe("Vinaigrette", RecipeCourse.Main, false, "1 shallot, minced", "2 tbsp lemon juice");
        var shallot = recipe.Ingredients[0];
        (shallot.Key, shallot.CatalogName, shallot.DisplayName) = ("onion", "onion", "onion"); // as the catalog once filed it
        core.Recipes.Rows.Add(recipe);
        var recipes = core.RecipeManager();

        var first = await recipes.RefreshIngredientNamesAsync(new RefreshIngredientNames(), default);
        var second = await recipes.RefreshIngredientNamesAsync(new RefreshIngredientNames(), default);

        var saved = core.Recipes.Rows.Single().Ingredients;
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(second).IsEqualTo(0);
        await Assert.That(saved[0].Key).IsEqualTo("shallot");
        await Assert.That(saved[0].CatalogName).IsEqualTo("onion");
        await Assert.That(saved[1].Key).IsEqualTo("lemon");
        await Assert.That(core.Messages.Published.OfType<IngredientNamesChanged>().Count()).IsEqualTo(1);
    }
}
