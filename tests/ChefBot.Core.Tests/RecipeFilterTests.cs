using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Narrowing the recipe library by tags, favourites, slow cooker and time, and what it can be narrowed by.</summary>
public class RecipeFilterTests
{
    private static Recipe Tagged(string name, int? minutes, bool slowCooker, params string[] tags)
    {
        var recipe = TestData.NewRecipe(name, RecipeCourse.Main, slowCooker, "1 cup water");
        recipe.Tags = [.. tags];
        recipe.TotalMinutes = minutes;
        return recipe;
    }

    private static readonly Recipe QuickVeg = Tagged("Quick Veg Stir Fry", 20, false, "quick", "Vegetarian");
    private static readonly Recipe QuickFish = Tagged("Quick Fish", 25, false, "quick");
    private static readonly Recipe SlowVeg = Tagged("Slow Veg Chili", 240, true, "vegetarian", "  ");
    private static readonly Recipe Untimed = Tagged("Untimed Pasta", null, false);

    private static (TestCore Core, UserAccount Jamie, UserAccount Chris) Household()
    {
        var core = new TestCore();
        core.Recipes.Rows.AddRange([QuickVeg, QuickFish, SlowVeg, Untimed]);
        var jamie = new UserAccount { Username = "jamie", DisplayName = "Jamie" };
        var chris = new UserAccount { Username = "chris", DisplayName = "Chris" };
        core.Accounts.Users.AddRange([jamie, chris]);
        return (core, jamie, chris);
    }

    private static async Task<string> Found(TestCore core, RecipeSearchRequest request) =>
        string.Join(", ", (await core.RecipeManager().SearchAsync(request, default)).Recipes.Select(recipe => recipe.Name).Order());

    private static RecipeSearchRequest All => new(null, null, 1, 100);

    [Test]
    public async Task Every_tag_asked_for_must_be_on_the_recipe_in_any_case()
    {
        var (core, _, _) = Household();

        await Assert.That(await Found(core, All with { Tags = ["quick"] })).IsEqualTo("Quick Fish, Quick Veg Stir Fry");
        await Assert.That(await Found(core, All with { Tags = ["QUICK", "vegetarian "] })).IsEqualTo("Quick Veg Stir Fry");
    }

    [Test]
    public async Task Favourites_can_be_anyones_or_one_persons()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();
        await recipes.AddFavouriteAsync(QuickFish.Id, jamie.Id, default);
        await recipes.AddFavouriteAsync(SlowVeg.Id, chris.Id, default);

        await Assert.That(await Found(core, All with { Favourites = true })).IsEqualTo("Quick Fish, Slow Veg Chili");
        await Assert.That(await Found(core, All with { FavouredBy = chris.Id })).IsEqualTo("Slow Veg Chili");
        await Assert.That(await Found(core, All with { FavouredBy = chris.Id, Tags = ["quick"] })).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Slow_cooker_and_time_narrow_the_list_and_recipes_without_a_time_are_left_out()
    {
        var (core, _, _) = Household();

        await Assert.That(await Found(core, All with { SlowCooker = true })).IsEqualTo("Slow Veg Chili");
        await Assert.That(await Found(core, All with { ReadyWithin = 20 })).IsEqualTo("Quick Veg Stir Fry");
        await Assert.That(await Found(core, All with { ReadyWithin = 0 })).IsEqualTo("Quick Fish, Quick Veg Stir Fry, Slow Veg Chili, Untimed Pasta");
    }

    [Test]
    public async Task The_filters_list_each_tag_once_most_used_first_and_the_people_with_favourites()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();
        core.Clock.Now = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        await recipes.AddFavouriteAsync(QuickFish.Id, chris.Id, default);
        core.Clock.Now = core.Clock.Now.AddMinutes(1);
        await recipes.AddFavouriteAsync(QuickVeg.Id, jamie.Id, default);
        await recipes.AddFavouriteAsync(SlowVeg.Id, chris.Id, default);

        var filters = await recipes.GetFiltersAsync(default);

        await Assert.That(string.Join(", ", filters.Tags.Select(tag => $"{tag.Name} {tag.Recipes}"))).IsEqualTo("quick 2, Vegetarian 2");
        await Assert.That(string.Join(", ", filters.People.Select(person => person.Name))).IsEqualTo("Chris, Jamie");
    }
}
