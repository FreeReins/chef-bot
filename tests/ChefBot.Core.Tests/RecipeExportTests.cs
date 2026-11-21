using System.Text;
using System.Text.Json;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class RecipeExportTests
{
    private static readonly RecipeDraft Tacos = new(
        "Slaw Fish Tacos",
        RecipeCourse.Main,
        ["600 g fish fillets", "1/2 head cabbage", "3 limes"],
        Servings: 4,
        Description: "Crisp fish and a sharp lime slaw.",
        Instructions: ["Shred the cabbage and dress it with lime.", "Roast the fish for 8 to 10 minutes."],
        PrepMinutes: 20,
        CookMinutes: 10,
        TotalMinutes: 90,
        SourceUrl: "https://example.com/slaw-fish-tacos",
        Author: "Jo Park",
        YieldText: "8 tacos",
        Notes: "Any firm white fish works.",
        Tags: ["quick", "fish"]);

    private static string Text(RecipeExportFile file) => Encoding.UTF8.GetString(file.Data);

    private static async Task<bool> Throws<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }

    [Test]
    public async Task A_recipe_is_written_in_schema_org_terms()
    {
        var recipes = new TestCore().RecipeManager();
        var saved = await recipes.CreateAsync(Tacos, default);

        var file = await recipes.ExportAsync(saved.Id, RecipeExportFormat.Json, default);

        await Assert.That(file.FileName).IsEqualTo("slaw-fish-tacos.json");
        await Assert.That(file.ContentType).IsEqualTo("application/ld+json");
        using var json = JsonDocument.Parse(file.Data);
        var root = json.RootElement;
        await Assert.That(root.GetProperty("@context").GetString()).IsEqualTo("https://schema.org");
        await Assert.That(root.GetProperty("@type").GetString()).IsEqualTo("Recipe");
        await Assert.That(root.GetProperty("recipeCategory").GetString()).IsEqualTo("Main");
        await Assert.That(root.GetProperty("keywords").GetString()).IsEqualTo("quick, fish");
        await Assert.That(root.GetProperty("prepTime").GetString()).IsEqualTo("PT20M");
        await Assert.That(root.GetProperty("totalTime").GetString()).IsEqualTo("PT1H30M");
        await Assert.That(root.GetProperty("recipeYield").EnumerateArray().Select(item => item.GetString() ?? string.Empty))
            .IsEquivalentTo(["4", "8 tacos"]);
        var step = root.GetProperty("recipeInstructions")[0];
        await Assert.That(step.GetProperty("@type").GetString()).IsEqualTo("HowToStep");
        await Assert.That(step.GetProperty("text").GetString()).IsEqualTo("Shred the cabbage and dress it with lime.");
        await Assert.That(root.GetProperty("author").GetProperty("name").GetString()).IsEqualTo("Jo Park");
    }

    [Test]
    public async Task An_exported_recipe_imports_back_unchanged()
    {
        var recipes = new TestCore().RecipeManager();
        var saved = await recipes.CreateAsync(Tacos with { IsSlowCooker = true }, default);

        var file = await recipes.ExportAsync(saved.Id, RecipeExportFormat.Json, default);
        var read = TestData.Transformation.ReadFile(file.FileName, Text(file)).Recipes.Single();

        await Assert.That(read.Name).IsEqualTo(Tacos.Name);
        await Assert.That(read.Course).IsEqualTo(RecipeCourse.Main);
        await Assert.That(read.IsSlowCooker).IsTrue();
        await Assert.That(read.Servings).IsEqualTo(4m);
        await Assert.That(read.Ingredients).IsEquivalentTo(Tacos.Ingredients);
        await Assert.That(read.Description).IsEqualTo(Tacos.Description);
        await Assert.That(read.Instructions!).IsEquivalentTo(Tacos.Instructions!);
        await Assert.That(read.PrepMinutes).IsEqualTo(20);
        await Assert.That(read.CookMinutes).IsEqualTo(10);
        await Assert.That(read.TotalMinutes).IsEqualTo(90);
        await Assert.That(read.SourceUrl).IsEqualTo(Tacos.SourceUrl);
        await Assert.That(read.Author).IsEqualTo(Tacos.Author);
        await Assert.That(read.YieldText).IsEqualTo(Tacos.YieldText);
        await Assert.That(read.Notes).IsEqualTo(Tacos.Notes);
        await Assert.That(read.Tags!).IsEquivalentTo(Tacos.Tags!); // the course isn't added as a tag
    }

    [Test]
    public async Task Exporting_everything_lists_each_recipe_that_can_be_planned_once()
    {
        var recipes = new TestCore().RecipeManager();
        await recipes.CreateAsync(Tacos, default);
        await recipes.CreateAsync(new RecipeDraft("Overnight Oats", RecipeCourse.Breakfast, ["1 cup oats", "1 cup milk"]), default);
        var archived = await recipes.CreateAsync(new RecipeDraft("Old Stew", RecipeCourse.Main, ["1 lb beef"]), default);
        await recipes.ArchiveAsync(archived.Id, default);

        var file = await recipes.ExportAllAsync(RecipeExportFormat.Json, default);
        var read = TestData.Transformation.ReadFile(file.FileName, Text(file));

        await Assert.That(file.FileName).StartsWith("chefbot-recipes-");
        await Assert.That(read.Recipes.Select(recipe => recipe.Name)).IsEquivalentTo(["Overnight Oats", "Slaw Fish Tacos"]);
        await Assert.That(read.Recipes.Single(recipe => recipe.Name == "Overnight Oats").Course).IsEqualTo(RecipeCourse.Breakfast);
    }

    [Test]
    public async Task An_archived_recipe_cant_be_exported()
    {
        var recipes = new TestCore().RecipeManager();
        var saved = await recipes.CreateAsync(Tacos, default);
        await recipes.ArchiveAsync(saved.Id, default);

        await Assert.That(await Throws<NotFoundException>(() => recipes.ExportAsync(saved.Id, RecipeExportFormat.Json, default))).IsTrue();
    }

    [Test]
    [Arguments("Slaw Fish Tacos!", "slaw-fish-tacos")]
    [Arguments("  Crème brûlée (easy)  ", "crème-brûlée-easy")]
    [Arguments("!!!", "recipe")]
    public async Task File_names_are_made_from_the_recipe_name(string name, string expected)
    {
        await Assert.That(TransformationEngine.FileSlug(name)).IsEqualTo(expected);
    }
}
