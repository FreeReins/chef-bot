using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class MealieSettingsTests
{
    private const string Address = "http://mealie.local:9000";

    private static MealieSettingsRequest Connect(string? url, string? token) => new(url, token);

    [Test]
    public async Task The_configuration_is_the_starting_value()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();

        await recipes.UseDefaultMealieConnectionAsync(Address, "token-from-env", CancellationToken.None);

        var saved = await recipes.GetMealieSettingsAsync(CancellationToken.None);
        await Assert.That(saved.Url).IsEqualTo(Address);
        await Assert.That(core.Settings.Mealie.ApiToken).IsEqualTo("token-from-env");
        await Assert.That(saved.IsConfigured).IsTrue();
    }

    [Test]
    public async Task A_saved_connection_wins_over_the_configuration()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await recipes.UpdateMealieSettingsAsync(Connect("https://recipes.example.com", "saved-token"), CancellationToken.None);

        await recipes.UseDefaultMealieConnectionAsync(Address, "token-from-env", CancellationToken.None);

        await Assert.That(core.Settings.Mealie.Url).IsEqualTo("https://recipes.example.com");
        await Assert.That(core.Settings.Mealie.ApiToken).IsEqualTo("saved-token");
    }

    [Test]
    public async Task An_address_without_a_token_in_the_configuration_is_ignored()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();

        await recipes.UseDefaultMealieConnectionAsync(Address, "", CancellationToken.None);

        var saved = await recipes.GetMealieSettingsAsync(CancellationToken.None);
        await Assert.That(saved.IsConfigured).IsFalse();
        await Assert.That(saved.Url).IsEqualTo("");
    }

    [Test]
    public async Task Leaving_the_token_empty_keeps_the_saved_one()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await recipes.UpdateMealieSettingsAsync(Connect(Address, "first-token"), CancellationToken.None);

        var saved = await recipes.UpdateMealieSettingsAsync(Connect("http://192.168.1.20:9000", null), CancellationToken.None);

        await Assert.That(saved.Url).IsEqualTo("http://192.168.1.20:9000");
        await Assert.That(saved.HasApiToken).IsTrue();
        await Assert.That(core.Settings.Mealie.ApiToken).IsEqualTo("first-token");
    }

    [Test]
    public async Task An_empty_address_turns_Mealie_off_and_forgets_the_token()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await recipes.UpdateMealieSettingsAsync(Connect(Address, "token"), CancellationToken.None);

        var saved = await recipes.UpdateMealieSettingsAsync(Connect("  ", null), CancellationToken.None);

        await Assert.That(saved.Url).IsEqualTo("");
        await Assert.That(saved.HasApiToken).IsFalse();
        await Assert.That(saved.IsConfigured).IsFalse();
    }

    [Test]
    public async Task An_address_needs_a_token_the_first_time()
    {
        var recipes = new TestCore().RecipeManager();

        await Assert.That(await TestData.Caught<RuleViolationException>(() => recipes.UpdateMealieSettingsAsync(Connect(Address, null), CancellationToken.None))).IsNotNull();
    }

    [Test]
    [Arguments("mealie.local:9000")]
    [Arguments("ftp://mealie.local")]
    [Arguments("not an address")]
    public async Task Only_web_addresses_are_accepted(string address)
    {
        var recipes = new TestCore().RecipeManager();

        await Assert.That(await TestData.Caught<RuleViolationException>(() => recipes.UpdateMealieSettingsAsync(Connect(address, "token"), CancellationToken.None))).IsNotNull();
    }

    [Test]
    public async Task A_failed_import_is_reported_with_the_saved_connection_and_changes_nothing()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await recipes.UpdateMealieSettingsAsync(Connect(Address, "token"), CancellationToken.None);

        var finished = await recipes.ImportFromMealieAsync(new ImportMealieRecipes(), CancellationToken.None);

        await Assert.That(finished.Succeeded).IsFalse();
        await Assert.That(finished.Problem).IsEqualTo("Not a real Mealie.");
        await Assert.That(core.Mealie.UsedUrl).IsEqualTo(Address);
        await Assert.That(core.Mealie.UsedToken).IsEqualTo("token");
        await Assert.That(core.Messages.Published.Contains(finished)).IsTrue();
        await Assert.That(core.Recipes.Rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Importing_without_a_connection_says_where_to_set_one_up()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();

        var finished = await recipes.ImportFromMealieAsync(new ImportMealieRecipes(), CancellationToken.None);

        await Assert.That(finished.Succeeded).IsFalse();
        await Assert.That(finished.Problem!.Contains("Settings tab")).IsTrue();
        await Assert.That(core.Mealie.UsedUrl).IsNull();
        await Assert.That(await TestData.Caught<RuleViolationException>(() => recipes.RequestMealieImportAsync(CancellationToken.None))).IsNotNull();
    }

    [Test]
    public async Task An_import_adds_refreshes_and_archives_and_carries_the_week_to_stage()
    {
        var core = new TestCore();
        var recipes = core.RecipeManager();
        await recipes.UpdateMealieSettingsAsync(Connect(Address, "token"), CancellationToken.None);
        core.Mealie.Recipes = [Source("1", "Chili"), Source("2", "Soup")];
        await recipes.ImportFromMealieAsync(new ImportMealieRecipes(), CancellationToken.None);

        // Chili is edited in ChefBot, so later imports leave it alone; Soup is gone from Mealie.
        var chili = core.Recipes.Rows.Single(r => r.Name == "Chili");
        await recipes.UpdateAsync(chili.Id, new RecipeDraft("Chili", RecipeCourse.Main, ["1 lb beans"]), CancellationToken.None);
        core.Mealie.Recipes = [Source("1", "Chili, renamed"), Source("3", "Stew")];

        var week = new DateOnly(2026, 11, 15);
        var finished = await recipes.ImportFromMealieAsync(new ImportMealieRecipes(StageWeekAfter: week), CancellationToken.None);

        await Assert.That(finished.Result).IsEqualTo(new ImportResult(Added: 1, Updated: 0, KeptLocal: 1, Archived: 1, Skipped: 0));
        await Assert.That(finished.StageWeekAfter).IsEqualTo(week);
        await Assert.That(core.Recipes.Rows.Single(r => r.ExternalId == "2").IsArchived).IsTrue();
        await Assert.That(core.Recipes.Rows.Single(r => r.ExternalId == "1").Name).IsEqualTo("Chili");
    }

    private static SourceRecipe Source(string id, string name) =>
        new(id, name, 4, [], [new SourceIngredient("kale", 1, "bunch", null)]);
}
