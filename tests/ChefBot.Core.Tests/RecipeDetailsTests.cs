using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Steps, times, metadata and photos on recipes: typed in, uploaded and imported.</summary>
public class RecipeDetailsTests
{
    private const string Address = "http://mealie.local:9000";

    private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    private static RecipeDraft Draft(string name = "Chili") => new(
        name,
        RecipeCourse.Main,
        ["1 lb ground beef, browned", "1 can beans"],
        Description: "  Weeknight chili. ",
        Instructions: ["Brown the beef.", "  ", "Simmer."],
        PrepMinutes: 10,
        CookMinutes: 40,
        TotalMinutes: 50,
        SourceUrl: "https://example.com/chili",
        Author: "Sam",
        YieldText: "one big pot",
        Notes: "Better the next day.",
        Tags: ["Spicy", "spicy", "quick"]);

    [Test]
    public async Task A_typed_in_recipe_keeps_its_details_and_ingredient_lines_as_written()
    {
        var recipes = new TestCore().RecipeManager();

        var chili = await recipes.CreateAsync(Draft(), CancellationToken.None);

        await Assert.That(chili.Description).IsEqualTo("Weeknight chili.");
        await Assert.That(chili.Instructions).IsEquivalentTo(["Brown the beef.", "Simmer."]);
        await Assert.That(chili.PrepMinutes).IsEqualTo(10);
        await Assert.That(chili.CookMinutes).IsEqualTo(40);
        await Assert.That(chili.TotalMinutes).IsEqualTo(50);
        await Assert.That(chili.SourceUrl).IsEqualTo("https://example.com/chili");
        await Assert.That(chili.Author).IsEqualTo("Sam");
        await Assert.That(chili.YieldText).IsEqualTo("one big pot");
        await Assert.That(chili.Notes).IsEqualTo("Better the next day.");
        await Assert.That(chili.Tags).IsEquivalentTo(["Spicy", "quick"]);
        await Assert.That(chili.Ingredients[0]).IsEqualTo("1 lb ground beef, browned");
        await Assert.That(chili.IngredientDetails[0].Quantity).IsEqualTo(1m);
        await Assert.That(chili.IngredientDetails[0].Unit).IsEqualTo("lb");
    }

    [Test]
    public async Task An_edit_replaces_the_details()
    {
        var recipes = new TestCore().RecipeManager();
        var chili = await recipes.CreateAsync(Draft(), CancellationToken.None);

        var edited = await recipes.UpdateAsync(chili.Id, new RecipeDraft("Chili", RecipeCourse.Main, ["1 can beans"], Instructions: ["Heat."]), CancellationToken.None);

        await Assert.That(edited.Instructions).IsEquivalentTo(["Heat."]);
        await Assert.That(edited.Description).IsNull();
        await Assert.That(edited.Tags).IsEmpty();
        await Assert.That(edited.Ingredients).IsEquivalentTo(["1 can beans"]);
    }

    [Test]
    public async Task Times_and_the_source_link_are_checked()
    {
        var recipes = new TestCore().RecipeManager();

        var negative = await TestData.Caught<RuleViolationException>(() => recipes.CreateAsync(Draft() with { PrepMinutes = -5 }, CancellationToken.None));
        var tooLong = await TestData.Caught<RuleViolationException>(() => recipes.CreateAsync(Draft() with { CookMinutes = 20000 }, CancellationToken.None));
        var badLink = await TestData.Caught<RuleViolationException>(() => recipes.CreateAsync(Draft() with { SourceUrl = "example.com/chili" }, CancellationToken.None));

        await Assert.That(negative!.Message).Contains("Prep time");
        await Assert.That(tooLong!.Message).Contains("Cook time");
        await Assert.That(badLink!.Message).Contains("http://");
    }

    [Test]
    public async Task An_uploaded_photo_is_stored_served_and_removed()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        var chili = await recipes.CreateAsync(Draft(), CancellationToken.None);

        var withPhoto = await recipes.AddImageAsync(chili.Id, new AddRecipeImageRequest(Png), CancellationToken.None);
        var photo = withPhoto.Images.Single();
        var served = await recipes.GetImageAsync(chili.Id, photo.Id, CancellationToken.None);

        await Assert.That(photo.Url).IsEqualTo($"/api/recipes/{chili.Id}/images/{photo.Id}");
        await Assert.That(served.ContentType).IsEqualTo("image/png");
        await Assert.That(served.Data).IsEquivalentTo(Png);

        var without = await recipes.RemoveImageAsync(chili.Id, photo.Id, CancellationToken.None);

        await Assert.That(without.Images).IsEmpty();
        await Assert.That(core.Recipes.ImageFiles).IsEmpty();
        await Assert.That(await TestData.Caught<NotFoundException>(() => recipes.GetImageAsync(chili.Id, photo.Id, CancellationToken.None))).IsNotNull();
    }

    [Test]
    public async Task Only_photos_of_a_sensible_size_are_accepted()
    {
        var recipes = new TestCore().RecipeManager();
        var chili = await recipes.CreateAsync(Draft(), CancellationToken.None);

        var text = await TestData.Caught<RuleViolationException>(() => recipes.AddImageAsync(chili.Id, new AddRecipeImageRequest("hello"u8.ToArray()), CancellationToken.None));
        var empty = await TestData.Caught<RuleViolationException>(() => recipes.AddImageAsync(chili.Id, new AddRecipeImageRequest(null), CancellationToken.None));
        var huge = new byte[(5 * 1024 * 1024) + 1];
        Png.CopyTo(huge, 0);
        var tooBig = await TestData.Caught<RuleViolationException>(() => recipes.AddImageAsync(chili.Id, new AddRecipeImageRequest(huge), CancellationToken.None));

        await Assert.That(text!.Message).Contains("JPEG, PNG, WebP or GIF");
        await Assert.That(empty!.Message).Contains("empty");
        await Assert.That(tooBig!.Message).Contains("5 MB");
    }

    [Test]
    public async Task Uploading_a_photo_makes_an_imported_recipe_a_local_one()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await ImportAsync(core, Source("1", "Stew"));
        var stew = core.Recipes.Rows.Single();

        var updated = await recipes.AddImageAsync(stew.Id, new AddRecipeImageRequest(Png), CancellationToken.None);

        await Assert.That(updated.Origin).IsEqualTo(RecipeOrigin.Local);
    }

    [Test]
    public async Task An_import_asks_for_its_photos_and_they_are_downloaded_afterwards()
    {
        var core = new TestCore();
        core.Images.Files["https://img/stew.png"] = Png;
        await ImportAsync(core, Source("1", "Stew", "https://img/stew.png", "https://img/missing.png"));
        var stew = core.Recipes.Rows.Single();

        var request = core.Messages.Published.OfType<FetchRecipeImages>().Single();
        await Assert.That(request.RecipeId).IsEqualTo(stew.Id);
        await Assert.That(stew.Images.Count).IsEqualTo(2);
        await Assert.That(stew.Images.Any(image => image.IsStored)).IsFalse();

        var problems = await core.RecipeManager().FetchImagesAsync(request, CancellationToken.None);

        var stored = core.Recipes.Rows.Single();
        await Assert.That(problems.Single()).Contains("missing.png");
        await Assert.That(stored.Images.Single(image => image.IsStored).SourceUrl).IsEqualTo("https://img/stew.png");
        await Assert.That(stored.Images.Single(image => !image.IsStored).SourceUrl).IsEqualTo("https://img/missing.png");
        var listed = (await core.RecipeManager().ListAsync(CancellationToken.None)).Single();
        await Assert.That(listed.Images.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_new_import_keeps_downloaded_photos_and_drops_ones_mealie_no_longer_has()
    {
        var core = new TestCore();
        core.Images.Files["https://img/a.png"] = Png;
        await ImportAsync(core, Source("1", "Stew", "https://img/a.png", "https://img/b.png"));
        await core.RecipeManager().FetchImagesAsync(new FetchRecipeImages(core.Recipes.Rows.Single().Id), CancellationToken.None);
        var kept = core.Recipes.Rows.Single().Images.Single(image => image.SourceUrl == "https://img/a.png");
        core.Messages.Published.Clear();
        core.Images.Requested.Clear();

        await ImportAsync(core, Source("1", "Stew", "https://img/a.png", "https://img/c.png"));

        var stew = core.Recipes.Rows.Single();
        await Assert.That(stew.Images.Select(image => image.SourceUrl!)).IsEquivalentTo(["https://img/a.png", "https://img/c.png"]);
        await Assert.That(stew.Images[0].Id).IsEqualTo(kept.Id);
        await Assert.That(stew.Images[0].IsStored).IsTrue();
        await Assert.That(core.Recipes.ImageFiles.ContainsKey(kept.Id)).IsTrue();
        await Assert.That(core.Messages.Published.OfType<FetchRecipeImages>().Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Imported_details_and_ingredient_notes_are_kept()
    {
        var core = new TestCore();
        var source = Source("1", "Stew") with
        {
            Ingredients = [new SourceIngredient("beef", 2, "lb", "2 lb beef, cubed", "cubed")],
            Instructions = ["Brown.", "Simmer."],
            PrepTime = "PT15M",
            CookTime = "2 hours",
            SourceUrl = "https://example.com/stew",
        };

        await ImportAsync(core, source);

        var stew = core.Recipes.Rows.Single();
        await Assert.That(stew.Instructions).IsEquivalentTo(["Brown.", "Simmer."]);
        await Assert.That(stew.PrepMinutes).IsEqualTo(15);
        await Assert.That(stew.CookMinutes).IsEqualTo(120);
        await Assert.That(stew.SourceUrl).IsEqualTo("https://example.com/stew");
        await Assert.That(stew.Tags).IsEquivalentTo(["Winter"]);
        await Assert.That(stew.Ingredients.Single().Text).IsEqualTo("2 lb beef, cubed");
        await Assert.That(stew.Ingredients.Single().Note).IsEqualTo("cubed");
    }

    private static async Task ImportAsync(TestCore core, params SourceRecipe[] recipes)
    {
        var manager = core.RecipeManager();
        await manager.UpdateMealieSettingsAsync(new MealieSettingsRequest(Address, "token"), CancellationToken.None);
        core.Mealie.Recipes = [.. recipes];
        await manager.ImportFromMealieAsync(new ImportMealieRecipes(), CancellationToken.None);
    }

    private static SourceRecipe Source(string id, string name, params string[] images) =>
        new(id, name, 4, ["Winter"], [new SourceIngredient("kale", 1, "bunch", null)], ImageUrls: images);
}
