using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Recipe health: what each recipe is missing, and names and tags to tidy up across the library.</summary>
public class RecipeHealthTests
{
    private static Recipe Complete(string name, params string[] tags)
    {
        var recipe = TestData.NewRecipe(name, RecipeCourse.Main, false, "1 cup quinoa", "2 cups chicken broth", "salt, to taste");
        recipe.Description = "Plain quinoa.";
        recipe.TotalMinutes = 20;
        recipe.Tags = tags.Length == 0 ? ["side"] : [.. tags];
        recipe.Instructions = ["Rinse the quinoa well under cold water until it runs clear.", "Simmer it in the broth with the lid on for eighteen minutes, then rest it off the heat for five minutes before serving."];
        recipe.Images = [new RecipeImage { ContentType = "image/jpeg" }];
        return recipe;
    }

    private static string Issues(Recipe recipe) => string.Join(", ", RecipeHealth.Issues(recipe));

    [Test]
    public async Task A_complete_recipe_has_no_issues_and_lines_that_need_no_amount_are_not_counted()
    {
        await Assert.That(Issues(Complete("Rice"))).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Ingredients_the_catalog_doesnt_know_are_not_held_against_the_recipe()
    {
        var recipe = Complete("Quinoa Bowl");
        recipe.Ingredients = TestData.Transformation.FromLines(["1 cup quinoa", "1/4 cup tahini", "2 tbsp hoisin sauce"]);

        await Assert.That(Issues(recipe)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task A_bare_recipe_has_every_issue()
    {
        var bare = TestData.NewRecipe("Mystery", RecipeCourse.Main, false, "zorblax fruit");

        await Assert.That(Issues(bare)).IsEqualTo("NoPhoto, NoDescription, NoTimes, NoTags, ThinSteps, MissingAmounts");
    }

    [Test]
    public async Task A_photo_still_downloading_counts_as_a_photo()
    {
        var recipe = Complete("Rice");
        recipe.Images = [new RecipeImage { SourceUrl = "https://example.com/rice.jpg" }];

        await Assert.That(Issues(recipe)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task The_report_lists_each_check_duplicates_and_rare_tags_leaving_archived_recipes_out()
    {
        var core = new TestCore();
        var rice = Complete("Rice", "side", "weeknight");
        var chili = Complete("Chili", "weeknight", "Spicy");
        var chiliAgain = Complete("chili!", "weeknight");
        chiliAgain.Description = null;
        var archived = TestData.NewRecipe("Old Chili", RecipeCourse.Main, false, "zorblax fruit");
        archived.IsArchived = true;
        archived.Tags = ["spicy"];
        core.Recipes.Rows.AddRange([rice, chili, chiliAgain, archived]);

        var health = await core.RecipeManager().GetHealthAsync(default);

        await Assert.That(health.Recipes).IsEqualTo(3);
        await Assert.That(health.Complete).IsEqualTo(2);
        await Assert.That(string.Join(", ", health.Checks.Select(check => $"{check.Issue} {check.Recipes.Count}")))
            .IsEqualTo("NoPhoto 0, NoDescription 1, NoTimes 0, NoTags 0, ThinSteps 0, MissingAmounts 0");
        await Assert.That(health.Checks[1].Recipes.Single().Name).IsEqualTo("chili!");
        await Assert.That(string.Join(" | ", health.Duplicates.Select(group => string.Join(", ", group.Recipes.Select(recipe => recipe.Name)))))
            .IsEqualTo("Chili, chili!");
        await Assert.That(string.Join(", ", health.RareTags.Select(rare => $"{rare.Tag} on {rare.Recipe.Name}"))).IsEqualTo("side on Rice, Spicy on Chili");
    }

    [Test]
    public async Task A_recipe_in_full_says_what_it_is_missing()
    {
        var core = new TestCore();
        var recipe = Complete("Rice");
        recipe.Tags = [];
        core.Recipes.Rows.Add(recipe);

        var response = await core.RecipeManager().GetAsync(recipe.Id, default);

        await Assert.That(string.Join(", ", response.Issues!)).IsEqualTo("NoTags");
    }
}
