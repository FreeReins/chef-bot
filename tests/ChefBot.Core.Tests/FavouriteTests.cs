using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class FavouriteTests
{
    private static (TestCore Core, UserAccount Jamie, UserAccount Chris) Household()
    {
        var core = new TestCore();
        core.Recipes.Rows.AddRange(TestData.Library);
        var jamie = new UserAccount { Username = "jamie", DisplayName = "Jamie", IsAdmin = true };
        var chris = new UserAccount { Username = "chris", DisplayName = "Chris" };
        core.Accounts.Users.AddRange([jamie, chris]);
        return (core, jamie, chris);
    }

    private static string Names(IEnumerable<FavouriteResponse> favouredBy) => string.Join(", ", favouredBy.Select(favourite => favourite.Name));

    [Test]
    public async Task Favouriting_twice_keeps_one_and_removing_twice_is_harmless()
    {
        var (core, jamie, _) = Household();
        var recipes = core.RecipeManager();

        await recipes.AddFavouriteAsync(TestData.Stew.Id, jamie.Id, default);
        var again = await recipes.AddFavouriteAsync(TestData.Stew.Id, jamie.Id, default);
        await Assert.That(core.Favourites.Rows.Count).IsEqualTo(1);
        await Assert.That(Names(again.FavouredBy)).IsEqualTo("Jamie");
        await Assert.That(again.FavouredBy[0].UserId).IsEqualTo(jamie.Id);

        await recipes.RemoveFavouriteAsync(TestData.Stew.Id, jamie.Id, default);
        var gone = await recipes.RemoveFavouriteAsync(TestData.Stew.Id, jamie.Id, default);
        await Assert.That(core.Favourites.Rows).IsEmpty();
        await Assert.That(gone.FavouredBy).IsEmpty();
    }

    [Test]
    public async Task Everyone_sees_everyones_favourites_in_search_and_on_the_recipe()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();
        core.Clock.Now = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
        await recipes.AddFavouriteAsync(TestData.Stew.Id, chris.Id, default);
        core.Clock.Now = core.Clock.Now.AddMinutes(5);
        await recipes.AddFavouriteAsync(TestData.Stew.Id, jamie.Id, default);

        var page = await recipes.SearchAsync(new RecipeSearchRequest(null, null, 1, 100), default);
        await Assert.That(Names(page.Recipes.Single(recipe => recipe.Id == TestData.Stew.Id).FavouredBy)).IsEqualTo("Chris, Jamie");
        await Assert.That(page.Recipes.Single(recipe => recipe.Id == TestData.Soup.Id).FavouredBy).IsEmpty();

        var stew = await recipes.GetAsync(TestData.Stew.Id, default);
        await Assert.That(Names(stew.FavouredBy)).IsEqualTo("Chris, Jamie");
    }

    [Test]
    public async Task An_archived_recipe_cant_be_favourited()
    {
        var (core, jamie, _) = Household();
        var recipes = core.RecipeManager();
        await recipes.ArchiveAsync(TestData.Stew.Id, default);

        await Assert.That(async () => await recipes.AddFavouriteAsync(TestData.Stew.Id, jamie.Id, default)).Throws<NotFoundException>();
        await Assert.That(core.Favourites.Rows).IsEmpty();
    }

    [Test]
    public async Task Removing_a_person_removes_their_favourites()
    {
        var (core, jamie, chris) = Household();
        var recipes = core.RecipeManager();
        await recipes.AddFavouriteAsync(TestData.Stew.Id, jamie.Id, default);
        await recipes.AddFavouriteAsync(TestData.Stew.Id, chris.Id, default);

        await core.AccountManager().RemoveUserAsync(chris.Id, jamie.Id, default);

        await Assert.That(core.Favourites.Rows.Select(row => row.UserId)).IsEquivalentTo([jamie.Id]);
    }
}
