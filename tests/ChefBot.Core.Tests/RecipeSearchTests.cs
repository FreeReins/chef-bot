using ChefBot.Core.Managers;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Finding recipes in the library by words and course, a page at a time, and opening one.</summary>
public class RecipeSearchTests
{
    private static async Task<RecipeManager> LibraryAsync()
    {
        var recipes = new TestCore().RecipeManager();
        await recipes.CreateAsync(new RecipeDraft("Chicken Curry", RecipeCourse.Main, ["1 lb chicken thigh", "1 can coconut milk"]), CancellationToken.None);
        await recipes.CreateAsync(new RecipeDraft("Lentil Soup", RecipeCourse.Main, ["1 cup lentils", "4 cups chicken broth"]), CancellationToken.None);
        await recipes.CreateAsync(new RecipeDraft("Overnight Oats", RecipeCourse.Breakfast, ["1 cup oats", "1 cup milk"], Tags: ["Make ahead"]), CancellationToken.None);
        await recipes.CreateAsync(new RecipeDraft("Maple Granola", RecipeCourse.Snack, ["3 cups oats", "0.5 cup maple syrup"]), CancellationToken.None);
        return recipes;
    }

    private static RecipeSearchRequest Search(string? query = null, RecipeCourse? course = null, int page = 1, int pageSize = 25) =>
        new(query, course, page, pageSize);

    private static IEnumerable<string> Names(RecipePageResponse page) => page.Recipes.Select(recipe => recipe.Name);

    [Test]
    public async Task No_query_lists_every_recipe_in_course_order_then_by_name()
    {
        var recipes = await LibraryAsync();

        var page = await recipes.SearchAsync(Search(), CancellationToken.None);

        await Assert.That(Names(page)).IsEquivalentTo(["Chicken Curry", "Lentil Soup", "Overnight Oats", "Maple Granola"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(page.Total).IsEqualTo(4);
        await Assert.That(page.LibrarySize).IsEqualTo(4);
        await Assert.That(page.Recipes[0].Ingredients).IsEquivalentTo(["1 lb chicken thigh", "1 can coconut milk"]);
    }

    [Test]
    public async Task A_word_matches_names_ingredients_and_tags_in_any_case()
    {
        var recipes = await LibraryAsync();

        var chicken = await recipes.SearchAsync(Search("CHICKEN"), CancellationToken.None);
        var tagged = await recipes.SearchAsync(Search("ahead"), CancellationToken.None);

        await Assert.That(Names(chicken)).IsEquivalentTo(["Chicken Curry", "Lentil Soup"]);
        await Assert.That(Names(tagged)).IsEquivalentTo(["Overnight Oats"]);
        await Assert.That(tagged.LibrarySize).IsEqualTo(4);
    }

    [Test]
    public async Task Every_word_must_match()
    {
        var recipes = await LibraryAsync();

        var page = await recipes.SearchAsync(Search("  oats   maple "), CancellationToken.None);

        await Assert.That(Names(page)).IsEquivalentTo(["Maple Granola"]);
        await Assert.That(page.Total).IsEqualTo(1);
    }

    [Test]
    public async Task A_course_narrows_the_search()
    {
        var recipes = await LibraryAsync();

        var page = await recipes.SearchAsync(Search("oats", RecipeCourse.Snack), CancellationToken.None);

        await Assert.That(Names(page)).IsEquivalentTo(["Maple Granola"]);
    }

    [Test]
    public async Task Archived_recipes_are_not_found_or_counted()
    {
        var recipes = await LibraryAsync();
        var oats = (await recipes.SearchAsync(Search("overnight"), CancellationToken.None)).Recipes[0];

        await recipes.ArchiveAsync(oats.Id, CancellationToken.None);
        var page = await recipes.SearchAsync(Search(), CancellationToken.None);

        await Assert.That(Names(page)).DoesNotContain("Overnight Oats");
        await Assert.That(page.LibrarySize).IsEqualTo(3);
        await Assert.That(async () => await recipes.GetAsync(oats.Id, CancellationToken.None)).Throws<NotFoundException>();
    }

    [Test]
    public async Task Pages_split_the_matches_and_keep_the_total()
    {
        var recipes = await LibraryAsync();

        var second = await recipes.SearchAsync(Search(page: 2, pageSize: 3), CancellationToken.None);
        var beyond = await recipes.SearchAsync(Search(page: 5, pageSize: 3), CancellationToken.None);

        await Assert.That(Names(second)).IsEquivalentTo(["Maple Granola"]);
        await Assert.That(second.Total).IsEqualTo(4);
        await Assert.That(second.Page).IsEqualTo(2);
        await Assert.That(beyond.Recipes).IsEmpty();
        await Assert.That(beyond.Total).IsEqualTo(4);
    }

    [Test]
    public async Task Out_of_range_pages_and_sizes_are_brought_into_range()
    {
        var recipes = await LibraryAsync();

        var unset = await recipes.SearchAsync(Search(page: 0, pageSize: 0), CancellationToken.None);
        var huge = await recipes.SearchAsync(Search(page: -3, pageSize: 5000), CancellationToken.None);

        await Assert.That(unset.Page).IsEqualTo(1);
        await Assert.That(unset.PageSize).IsEqualTo(25);
        await Assert.That(huge.Page).IsEqualTo(1);
        await Assert.That(huge.PageSize).IsEqualTo(100);
    }

    [Test]
    public async Task One_recipe_opens_in_full()
    {
        var recipes = await LibraryAsync();
        var curry = (await recipes.SearchAsync(Search("curry"), CancellationToken.None)).Recipes[0];

        var full = await recipes.GetAsync(curry.Id, CancellationToken.None);

        await Assert.That(full.Name).IsEqualTo("Chicken Curry");
        await Assert.That(full.IngredientDetails.Count).IsEqualTo(2);
        await Assert.That(async () => await recipes.GetAsync(Guid.NewGuid(), CancellationToken.None)).Throws<NotFoundException>();
    }

    [Test]
    public async Task A_recipe_in_the_list_carries_its_first_photo_as_its_cover()
    {
        var recipes = await LibraryAsync();
        var curry = (await recipes.SearchAsync(Search("curry"), CancellationToken.None)).Recipes[0];
        byte[] png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

        var first = await recipes.AddImageAsync(curry.Id, new AddRecipeImageRequest(png), CancellationToken.None);
        await recipes.AddImageAsync(curry.Id, new AddRecipeImageRequest(png), CancellationToken.None);
        var page = await recipes.SearchAsync(Search(), CancellationToken.None);

        await Assert.That(curry.CoverUrl).IsNull();
        await Assert.That(page.Recipes.Single(recipe => recipe.Id == curry.Id).CoverUrl).IsEqualTo(first.Images[0].Url);
        await Assert.That(page.Recipes.Single(recipe => recipe.Name == "Lentil Soup").CoverUrl).IsNull();
    }
}
