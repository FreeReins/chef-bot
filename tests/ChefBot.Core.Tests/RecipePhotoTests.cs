using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Reading a recipe from photos with an AI model, into a draft to check before saving.</summary>
public class RecipePhotoTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    private const string Card = """
        {"found":true,"name":"Grandma's Oat Cookies","course":"Sweet","slowCooker":false,"servings":0,"yield":"24 cookies",
         "description":"","ingredients":["2 cups rolled oats","1 cup butter, softened"],"instructions":["Cream the butter.","Bake 12 minutes at 350F."],
         "prepMinutes":15,"cookMinutes":12,"totalMinutes":0,"author":"Grandma June","notes":"","tags":["cookies"]}
        """;

    private static TestCore WithModels(bool photoModel = false)
    {
        var core = new TestCore();
        var claude = new AiModel { Name = "Claude", Provider = AiProvider.Anthropic, Model = "claude-opus-5-5", ApiKey = "k", IsDefault = true };
        var ollama = new AiModel { Name = "Ollama", Provider = AiProvider.OpenAiCompatible, Model = "qwen2.5vl", Endpoint = "http://o/v1", Position = 1 };
        core.Settings.Ai = new AiSettings
        {
            Models = [claude, ollama],
            Routes = photoModel ? [new AiRoute { Task = AiTask.RecipeFromPhoto, ModelId = ollama.Id }] : [],
        };
        core.Ai.Answers[RecipePhotoPrompt.SchemaName] = Card;
        return core;
    }

    [Test]
    public async Task A_card_reads_into_a_draft_and_nothing_is_saved()
    {
        var core = WithModels();

        var read = await core.RecipeManager().ReadRecipePhotoAsync(new([Jpeg, Jpeg]), default);

        await Assert.That(read.ReadWith).IsEqualTo("Claude");
        await Assert.That(read.Draft.Name).IsEqualTo("Grandma's Oat Cookies");
        await Assert.That(read.Draft.Course).IsEqualTo(RecipeCourse.Sweet);
        await Assert.That(string.Join(" | ", read.Draft.Ingredients)).IsEqualTo("2 cups rolled oats | 1 cup butter, softened");
        await Assert.That(read.Draft.Servings).IsNull();
        await Assert.That(read.Draft.YieldText).IsEqualTo("24 cookies");
        await Assert.That(read.Draft.TotalMinutes).IsNull();
        await Assert.That(read.Draft.Author).IsEqualTo("Grandma June");
        await Assert.That(core.Ai.Asked.Single().Question.Images.Count).IsEqualTo(2);
        await Assert.That(core.Ai.Asked.Single().Question.Images[0].MediaType).IsEqualTo("image/jpeg");
        await Assert.That(core.Recipes.Rows).IsEmpty();
        await Assert.That(core.Messages.Commits).IsEqualTo(0);
    }

    [Test]
    public async Task The_model_chosen_for_photos_reads_them()
    {
        var core = WithModels(photoModel: true);

        var read = await core.RecipeManager().ReadRecipePhotoAsync(new([Jpeg]), default);

        await Assert.That(read.ReadWith).IsEqualTo("Ollama");
    }

    [Test]
    public async Task Without_a_model_it_says_how_to_set_one_up()
    {
        var core = new TestCore();

        var error = (await TestData.Caught<RuleViolationException>(() => core.RecipeManager().ReadRecipePhotoAsync(new([Jpeg]), default)))!;

        await Assert.That(error.Message).Contains("Settings, AI assistants");
    }

    [Test]
    public async Task Photos_must_be_one_to_four_images()
    {
        var recipes = WithModels().RecipeManager();

        var none = (await TestData.Caught<RuleViolationException>(() => recipes.ReadRecipePhotoAsync(new([]), default)))!;
        var many = (await TestData.Caught<RuleViolationException>(() => recipes.ReadRecipePhotoAsync(new([Jpeg, Jpeg, Jpeg, Jpeg, Jpeg]), default)))!;
        var text = (await TestData.Caught<RuleViolationException>(() => recipes.ReadRecipePhotoAsync(new(["hello"u8.ToArray()]), default)))!;

        await Assert.That(none.Message).Contains("Choose a photo");
        await Assert.That(many.Message).Contains("up to 4");
        await Assert.That(text.Message).Contains("JPEG");
    }

    [Test]
    public async Task A_photo_without_a_recipe_says_what_to_try()
    {
        var core = WithModels();
        core.Ai.Answers[RecipePhotoPrompt.SchemaName] = """{"found":false,"name":"","ingredients":[]}""";

        var error = (await TestData.Caught<RuleViolationException>(() => core.RecipeManager().ReadRecipePhotoAsync(new([Jpeg]), default)))!;

        await Assert.That(error.Message).Contains("sharper photo");
    }

    [Test]
    public async Task A_model_that_fails_is_reported_as_it_said()
    {
        var core = WithModels();
        core.Ai.Failure = new AiUnavailableException("Claude is busy or couldn't be reached. Try again later.", true);

        var error = (await TestData.Caught<AiUnavailableException>(() => core.RecipeManager().ReadRecipePhotoAsync(new([Jpeg]), default)))!;

        await Assert.That(error.IsTransient).IsTrue();
    }

    [Test]
    public async Task An_unknown_course_falls_back_to_the_household_words()
    {
        var text = TransformationEngine.ReadPhotoAnswer(
            """{"found":true,"name":"Overnight oats","course":"Brunch","ingredients":["1 cup oats"],"tags":["breakfast"],"servings":"2"}""",
            new ClassifierWords())!;

        await Assert.That(text.Course).IsEqualTo(RecipeCourse.Breakfast);
        await Assert.That(text.Servings).IsEqualTo(2m);
    }
}
