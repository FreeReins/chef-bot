using System.Net;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

public class WebRecipeTests
{
    private const string SoupUrl = "https://blog.example/recipes/soup";

    private static readonly TransformationEngine Reader = TestData.Transformation;

    private static string Page(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Pages", name));

    private static string JsonLdPage(string json) => $"""
        <!doctype html><html><head><title>A page</title>
        <script type="application/ld+json">{json}</script>
        </head><body><h1>Something else</h1></body></html>
        """;

    private const string Soup = """
        { "@context": "https://schema.org", "@type": "Recipe", "name": "Tomato &amp; Basil Soup",
          "recipeCategory": "Dinner", "recipeYield": ["4", "4 bowls"], "prepTime": "PT15M",
          "image": ["/img/soup.jpg", "https://cdn.example/soup-wide.jpg"],
          "recipeIngredient": ["2 lb tomatoes", "1 cup <b>fresh</b> basil", "  "],
          "recipeInstructions": [{ "@type": "HowToStep", "text": "Roast the tomatoes." },
                                 { "@type": "HowToStep", "text": "Blend with the basil." }] }
        """;

    private static RecipeText Read(string html, string url = SoupUrl)
    {
        var address = new Uri(url);
        return Reader.ReadPage(Reader.PageReader(address), address, new WebPage(address, html));
    }

    private static (TestCore Core, Managers.RecipeManager Recipes) WithPage(string url, string html)
    {
        var core = new TestCore();
        core.Web.Pages[url] = html;
        return (core, core.RecipeManager());
    }

    [Test]
    public async Task Reads_a_json_ld_recipe_from_a_page()
    {
        var soup = Read(JsonLdPage(Soup), SoupUrl + "#comments");

        await Assert.That(soup.Name).IsEqualTo("Tomato & Basil Soup");
        await Assert.That(soup.Ingredients).IsEquivalentTo(["2 lb tomatoes", "1 cup fresh basil"]);
        await Assert.That(soup.Instructions!).IsEquivalentTo(["Roast the tomatoes.", "Blend with the basil."]);
        await Assert.That(soup.Servings).IsEqualTo(4m);
        await Assert.That(soup.YieldText).IsEqualTo("4 bowls");
        await Assert.That(soup.PrepMinutes).IsEqualTo(15);
        await Assert.That(soup.Course).IsEqualTo(RecipeCourse.Main);
        await Assert.That(soup.SourceUrl).IsEqualTo(SoupUrl);
        await Assert.That(soup.ImageUrls!).IsEquivalentTo(["https://blog.example/img/soup.jpg", "https://cdn.example/soup-wide.jpg"]);
    }

    [Test]
    public async Task Finds_the_recipe_in_a_graph_after_a_broken_script()
    {
        var html = $$"""
            <html><head>
            <script type="application/ld+json">{ not json</script>
            <script type="application/ld+json">{ "@type": "Organization", "name": "A Blog" }</script>
            <script type="application/LD+JSON">{ "@context": "https://schema.org", "@graph": [
              { "@type": "WebPage", "name": "Soup page" },
              {{Soup}} ] }</script>
            </head></html>
            """;

        await Assert.That(Read(html).Name).IsEqualTo("Tomato & Basil Soup");
    }

    [Test]
    public async Task Finds_a_recipe_that_is_a_pages_main_entity()
    {
        var html = JsonLdPage($$"""{ "@type": "WebPage", "name": "Soup page", "mainEntity": {{Soup}} }""");

        await Assert.That(Read(html).Name).IsEqualTo("Tomato & Basil Soup");
    }

    [Test]
    public async Task Reads_a_microdata_recipe_when_there_is_no_json_ld()
    {
        var soup = Read(Page("microdata-recipe.html"), "https://made-up.example/lentil-soup");

        await Assert.That(soup.Name).IsEqualTo("Lentil Soup");
        await Assert.That(soup.Ingredients).IsEquivalentTo(["1 cup red lentils", "2 carrots, diced", "4 cups stock"]);
        await Assert.That(soup.Instructions!.Count).IsEqualTo(2);
        await Assert.That(soup.Description).IsEqualTo("A warming soup & a pantry staple.");
        await Assert.That(soup.Author).IsEqualTo("Sam Cook");
        await Assert.That(soup.Servings).IsEqualTo(6m);
        await Assert.That(soup.PrepMinutes).IsEqualTo(10);
        await Assert.That(soup.CookMinutes).IsEqualTo(40);
        await Assert.That(soup.ImageUrls!.Single()).IsEqualTo("https://made-up.example/photos/lentil-soup.jpg");
    }

    [Test]
    public async Task A_page_without_a_recipe_says_so()
    {
        var error = Assert.Throws<RuleViolationException>(() => Read("<html><body><h1>Ten soups to try</h1></body></html>"));

        await Assert.That(error!.Message).Contains("couldn't find a recipe");
    }

    [Test]
    public async Task A_site_reader_comes_before_the_one_for_any_site()
    {
        var engine = new TransformationEngine(
            TestData.Catalog, TestData.Options, TestData.Classifier, [], [new FakeSitePage(TestData.Schema), new SchemaOrgPage(TestData.Schema)]);
        var fussy = new Uri("https://www.fussy.example/soup/");

        var reader = engine.PageReader(fussy);
        var page = JsonLdPage(Soup).Replace("<h1>", "<ul><li class=\"ingredient\">3 lb tomatoes</li></ul><h1>", StringComparison.Ordinal);
        var soup = engine.ReadPage(reader, fussy, new WebPage(reader.PageToFetch(fussy), page));

        await Assert.That(reader.Name).IsEqualTo("Fussy Example");
        await Assert.That(reader.PageToFetch(fussy).AbsoluteUri).IsEqualTo("https://www.fussy.example/soup/print");
        await Assert.That(soup.Ingredients.Single()).IsEqualTo("3 lb tomatoes");
        await Assert.That(soup.SourceUrl).IsEqualTo("https://www.fussy.example/soup/");
        await Assert.That(engine.PageReader(new Uri(SoupUrl)).Name).IsEqualTo("Any site (schema.org)");
    }

    [Test]
    public async Task Importing_a_page_saves_the_recipe_and_asks_for_its_photos()
    {
        var (core, recipes) = WithPage(SoupUrl, JsonLdPage(Soup));

        var result = await recipes.ImportFromWebAsync(new WebRecipeRequest(" blog.example/recipes/soup "), CancellationToken.None);

        var saved = core.Recipes.Rows.Single();
        await Assert.That(core.Web.Requested.Single().AbsoluteUri).IsEqualTo(SoupUrl);
        await Assert.That(result.ReadWith).IsEqualTo("Any site (schema.org)");
        await Assert.That(result.Recipe.Name).IsEqualTo("Tomato & Basil Soup");
        await Assert.That(saved.Origin).IsEqualTo(RecipeOrigin.Local);
        await Assert.That(saved.SourceUrl).IsEqualTo(SoupUrl);
        await Assert.That(saved.Images.Count).IsEqualTo(2);
        await Assert.That(core.Messages.Published.OfType<FetchRecipeImages>().Single().RecipeId).IsEqualTo(saved.Id);
        await Assert.That(core.Messages.Commits).IsEqualTo(1);
    }

    [Test]
    public async Task The_same_page_or_name_is_not_imported_twice()
    {
        var (core, recipes) = WithPage(SoupUrl, JsonLdPage(Soup));
        core.Web.Pages["https://other.example/soup"] = JsonLdPage(Soup);
        await recipes.ImportFromWebAsync(new WebRecipeRequest(SoupUrl), CancellationToken.None);

        var samePage = await TestData.Caught<ConflictException>(() =>
            core.RecipeManager().ImportFromWebAsync(new WebRecipeRequest("https://www.blog.example/recipes/soup/"), CancellationToken.None));
        var sameName = await TestData.Caught<ConflictException>(() =>
            core.RecipeManager().ImportFromWebAsync(new WebRecipeRequest("https://other.example/soup"), CancellationToken.None));

        await Assert.That(samePage?.Message).Contains("That page is already in your recipes");
        await Assert.That(sameName?.Message).Contains("Tomato & Basil Soup is already in your recipes");
        await Assert.That(core.Recipes.Rows.Count).IsEqualTo(1);
        await Assert.That(core.Web.Requested.Select(page => page.Host)).IsEquivalentTo(["blog.example", "other.example"]);
    }

    [Test]
    [Arguments("")]
    [Arguments("soup")]
    [Arguments("ftp://blog.example/soup")]
    public async Task An_address_that_isnt_a_web_page_is_refused(string url)
    {
        var (core, recipes) = WithPage(SoupUrl, JsonLdPage(Soup));

        var error = await TestData.Caught<RuleViolationException>(() => recipes.ImportFromWebAsync(new WebRecipeRequest(url), CancellationToken.None));

        await Assert.That(error).IsNotNull();
        await Assert.That(core.Web.Requested.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Only_photos_from_the_mealie_server_may_be_on_a_private_network()
    {
        var core = new TestCore();
        core.Settings.Mealie = new MealieSettings { Url = "http://mealie.lan:9000", ApiToken = "token" };
        var recipe = TestData.NewRecipe("Stew", RecipeCourse.Main, false, "1 lb beef");
        recipe.Images.Add(new RecipeImage { SourceUrl = "http://mealie.lan:9000/api/media/recipes/1/images/original.webp" });
        recipe.Images.Add(new RecipeImage { SourceUrl = "http://mealie.lan/elsewhere.png" });
        core.Recipes.Rows.Add(recipe);

        await core.RecipeManager().FetchImagesAsync(new FetchRecipeImages(recipe.Id), CancellationToken.None);

        await Assert.That(core.Images.Requested.Count).IsEqualTo(2);
        await Assert.That(core.Images.PrivateAllowed.Single()).IsEqualTo("http://mealie.lan:9000/api/media/recipes/1/images/original.webp");
    }

    [Test]
    [Arguments("8.8.8.8", true)]
    [Arguments("2606:4700:4700::1111", true)]
    [Arguments("127.0.0.1", false)]
    [Arguments("10.1.2.3", false)]
    [Arguments("172.20.0.1", false)]
    [Arguments("192.168.1.10", false)]
    [Arguments("169.254.169.254", false)]
    [Arguments("100.100.1.1", false)]
    [Arguments("0.0.0.0", false)]
    [Arguments("::1", false)]
    [Arguments("fd00::1", false)]
    [Arguments("fe80::1", false)]
    [Arguments("::ffff:192.168.1.10", false)]
    public async Task Only_public_internet_addresses_are_downloaded_from(string address, bool isPublic)
    {
        await Assert.That(PublicWeb.IsPublic(IPAddress.Parse(address))).IsEqualTo(isPublic);
    }
}
