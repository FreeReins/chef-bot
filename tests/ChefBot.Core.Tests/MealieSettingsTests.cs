using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Recipes;

namespace ChefBot.Core.Tests;

public class MealieSettingsTests
{
    private const string Address = "http://mealie.local:9000";

    private static RecipeImportService Service(out TestData.FakeSettings settings, FakeSource? source = null)
    {
        settings = new TestData.FakeSettings();
        return new RecipeImportService(
            source ?? new FakeSource(), null!, settings, new NoDatabase(), new RecipeClassifier(new ClassifierOptions()),
            new RecipeIngredientBuilder(TestData.Catalog), TestData.Options, TimeProvider.System);
    }

    [Test]
    public async Task The_configuration_is_the_starting_value()
    {
        var imports = Service(out _);

        await imports.UseDefaultConnectionAsync(Address, "token-from-env", CancellationToken.None);

        var saved = await imports.GetSettingsAsync(CancellationToken.None);
        await Assert.That(saved.Url).IsEqualTo(Address);
        await Assert.That(saved.ApiToken).IsEqualTo("token-from-env");
        await Assert.That(saved.IsConfigured).IsTrue();
    }

    [Test]
    public async Task A_saved_connection_wins_over_the_configuration()
    {
        var imports = Service(out _);
        await imports.UpdateSettingsAsync("https://recipes.example.com", "saved-token", CancellationToken.None);

        await imports.UseDefaultConnectionAsync(Address, "token-from-env", CancellationToken.None);

        var saved = await imports.GetSettingsAsync(CancellationToken.None);
        await Assert.That(saved.Url).IsEqualTo("https://recipes.example.com");
        await Assert.That(saved.ApiToken).IsEqualTo("saved-token");
    }

    [Test]
    public async Task An_address_without_a_token_in_the_configuration_is_ignored()
    {
        var imports = Service(out _);

        await imports.UseDefaultConnectionAsync(Address, "", CancellationToken.None);

        await Assert.That(await imports.IsConfiguredAsync(CancellationToken.None)).IsFalse();
        await Assert.That((await imports.GetSettingsAsync(CancellationToken.None)).Url).IsEqualTo("");
    }

    [Test]
    public async Task Leaving_the_token_empty_keeps_the_saved_one()
    {
        var imports = Service(out _);
        await imports.UpdateSettingsAsync(Address, "first-token", CancellationToken.None);

        var saved = await imports.UpdateSettingsAsync("http://192.168.1.20:9000", null, CancellationToken.None);

        await Assert.That(saved.Url).IsEqualTo("http://192.168.1.20:9000");
        await Assert.That(saved.ApiToken).IsEqualTo("first-token");
    }

    [Test]
    public async Task An_empty_address_turns_Mealie_off_and_forgets_the_token()
    {
        var imports = Service(out _);
        await imports.UpdateSettingsAsync(Address, "token", CancellationToken.None);

        var saved = await imports.UpdateSettingsAsync("  ", null, CancellationToken.None);

        await Assert.That(saved.Url).IsEqualTo("");
        await Assert.That(saved.ApiToken).IsEqualTo("");
        await Assert.That(saved.IsConfigured).IsFalse();
    }

    [Test]
    public async Task An_address_needs_a_token_the_first_time()
    {
        var imports = Service(out _);

        await Assert.That(await Caught<RuleViolationException>(() => imports.UpdateSettingsAsync(Address, null, CancellationToken.None))).IsNotNull();
    }

    [Test]
    [Arguments("mealie.local:9000")]
    [Arguments("ftp://mealie.local")]
    [Arguments("not an address")]
    public async Task Only_web_addresses_are_accepted(string address)
    {
        var imports = Service(out _);

        await Assert.That(await Caught<RuleViolationException>(() => imports.UpdateSettingsAsync(address, "token", CancellationToken.None))).IsNotNull();
    }

    [Test]
    public async Task Importing_uses_the_saved_connection()
    {
        var source = new FakeSource();
        var imports = Service(out _, source);
        await imports.UpdateSettingsAsync(Address, "token", CancellationToken.None);

        await Assert.That(await Caught<RecipeSourceException>(() => imports.ImportAsync(CancellationToken.None))).IsNotNull();

        await Assert.That(source.UsedUrl).IsEqualTo(Address);
        await Assert.That(source.UsedToken).IsEqualTo("token");
    }

    [Test]
    public async Task Importing_without_a_connection_says_where_to_set_one_up()
    {
        var source = new FakeSource();
        var imports = Service(out _, source);

        var failure = await Caught<RecipeSourceException>(() => imports.ImportAsync(CancellationToken.None));

        await Assert.That(failure).IsNotNull();
        await Assert.That(failure!.Message.Contains("Settings tab")).IsTrue();
        await Assert.That(source.UsedUrl).IsNull();
    }

    /// <summary>The exception <paramref name="action"/> threw, or null when it didn't throw one of that type.</summary>
    private static async Task<TException?> Caught<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
            return null;
        }
        catch (TException exception)
        {
            return exception;
        }
    }

    /// <summary>Records the connection it was given, then fails so no recipes need storing.</summary>
    private sealed class FakeSource : IRecipeSource
    {
        public string? UsedUrl { get; private set; }

        public string? UsedToken { get; private set; }

        public Task<RecipeSourceResult> FetchAllAsync(MealieSettings connection, CancellationToken cancellationToken)
        {
            UsedUrl = connection.Url;
            UsedToken = connection.ApiToken;
            throw new RecipeSourceException("Not a real Mealie.");
        }
    }

    private sealed class NoDatabase : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
