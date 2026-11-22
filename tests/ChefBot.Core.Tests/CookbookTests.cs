using System.Text;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Tests;

/// <summary>Cookbooks: named groups of recipes, which recipes can be in any number of, or none.</summary>
public class CookbookTests
{
    private static (TestCore Core, Recipe Soup, Recipe Stew, Recipe Salad) WithRecipes()
    {
        var core = new TestCore();
        var soup = TestData.NewRecipe("Soup", RecipeCourse.Main, false, "2 cups broth");
        var stew = TestData.NewRecipe("Stew", RecipeCourse.Main, true, "1 lb beef");
        var salad = TestData.NewRecipe("Salad", RecipeCourse.Main, false, "1 head lettuce");
        core.Recipes.Rows.AddRange([soup, stew, salad]);
        return (core, soup, stew, salad);
    }

    [Test]
    public async Task A_new_library_has_no_cookbooks_so_pages_can_leave_them_out()
    {
        var (core, _, _, _) = WithRecipes();

        var filters = await core.RecipeManager().GetFiltersAsync(default);

        await Assert.That(filters.Cookbooks).IsEmpty();
        await Assert.That(await core.RecipeManager().ListCookbooksAsync(default)).IsEmpty();
    }

    [Test]
    public async Task A_cookbook_is_made_renamed_and_described_with_its_name_tidied()
    {
        var (core, _, _, _) = WithRecipes();
        var recipes = core.RecipeManager();

        var made = await recipes.CreateCookbookAsync(new CookbookDraft("  Weeknight   dinners ", "  "), default);
        var renamed = await recipes.UpdateCookbookAsync(made.Id, new CookbookDraft("Weeknight", "Quick ones."), default);

        await Assert.That(made.Name).IsEqualTo("Weeknight dinners");
        await Assert.That(made.Description).IsNull();
        await Assert.That(renamed.Name).IsEqualTo("Weeknight");
        await Assert.That(renamed.Description).IsEqualTo("Quick ones.");
        await Assert.That(core.Cookbooks.Rows.Single().Name).IsEqualTo("Weeknight");
        await Assert.That(core.Messages.Published.OfType<CookbooksChanged>().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task Names_must_be_given_and_unique_in_any_case_but_a_cookbook_keeps_its_own()
    {
        var (core, _, _, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var weeknight = await recipes.CreateCookbookAsync(new CookbookDraft("Weeknight"), default);

        var taken = await Assert.That(async () => await recipes.CreateCookbookAsync(new CookbookDraft("WEEKNIGHT"), default))
            .Throws<RuleViolationException>();
        var empty = await Assert.That(async () => await recipes.CreateCookbookAsync(new CookbookDraft(" "), default))
            .Throws<RuleViolationException>();
        var kept = await recipes.UpdateCookbookAsync(weeknight.Id, new CookbookDraft("weeknight"), default);

        await Assert.That(taken!.Message).IsEqualTo("There's already a cookbook called Weeknight. Pick another name.");
        await Assert.That(empty!.Message).IsEqualTo("Give the cookbook a name.");
        await Assert.That(kept.Name).IsEqualTo("weeknight");
    }

    [Test]
    public async Task Recipes_are_added_once_and_taken_out_again_and_counts_leave_out_archived_ones()
    {
        var (core, soup, stew, salad) = WithRecipes();
        var recipes = core.RecipeManager();
        var cookbook = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);

        await recipes.AddToCookbookAsync(cookbook.Id, new CookbookRecipesRequest([soup.Id, stew.Id, salad.Id]), default);
        var again = await recipes.AddToCookbookAsync(cookbook.Id, new CookbookRecipesRequest([soup.Id]), default);
        var removed = await recipes.RemoveFromCookbookAsync(cookbook.Id, salad.Id, default);
        await recipes.ArchiveAsync(stew.Id, default);
        var listed = (await recipes.ListCookbooksAsync(default)).Single();

        await Assert.That(again.Recipes).IsEqualTo(3);
        await Assert.That(core.Recipes.Rows.Single(row => row.Id == soup.Id).CookbookIds).IsEquivalentTo([cookbook.Id]);
        await Assert.That(removed.Recipes).IsEqualTo(2);
        await Assert.That(listed.Recipes).IsEqualTo(1);
    }

    [Test]
    public async Task Only_recipes_in_the_library_and_cookbooks_that_exist_can_be_used()
    {
        var (core, soup, stew, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var cookbook = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);
        await recipes.ArchiveAsync(stew.Id, default);

        await Assert.That(async () => await recipes.AddToCookbookAsync(cookbook.Id, new CookbookRecipesRequest([stew.Id]), default))
            .Throws<NotFoundException>();
        await Assert.That(async () => await recipes.AddToCookbookAsync(cookbook.Id, new CookbookRecipesRequest([]), default))
            .Throws<RuleViolationException>();
        await Assert.That(async () => await recipes.AddToCookbookAsync(Guid.NewGuid(), new CookbookRecipesRequest([soup.Id]), default))
            .Throws<NotFoundException>();
        await Assert.That(async () => await recipes.CreateAsync(new RecipeDraft("Pie", RecipeCourse.Sweet, ["1 apple"], CookbookIds: [Guid.NewGuid()]), default))
            .Throws<NotFoundException>();
    }

    [Test]
    public async Task A_recipe_is_saved_in_cookbooks_and_an_edit_without_them_leaves_them_alone()
    {
        var (core, _, _, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var winter = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);
        var baking = await recipes.CreateCookbookAsync(new CookbookDraft("Baking"), default);

        var pie = await recipes.CreateAsync(new RecipeDraft("Pie", RecipeCourse.Sweet, ["1 apple"], CookbookIds: [winter.Id, baking.Id, winter.Id]), default);
        var edited = await recipes.UpdateAsync(pie.Id, new RecipeDraft("Apple Pie", RecipeCourse.Sweet, ["2 apples"]), default);
        var emptied = await recipes.UpdateAsync(pie.Id, new RecipeDraft("Apple Pie", RecipeCourse.Sweet, ["2 apples"], CookbookIds: []), default);

        await Assert.That(string.Join(", ", pie.Cookbooks!.Select(cookbook => cookbook.Name))).IsEqualTo("Baking, Winter");
        await Assert.That(edited.Cookbooks!.Count).IsEqualTo(2);
        await Assert.That(emptied.Cookbooks!).IsEmpty();
    }

    [Test]
    public async Task Removing_a_cookbook_leaves_its_recipes_in_the_library_and_in_other_cookbooks()
    {
        var (core, soup, stew, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var winter = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);
        var quick = await recipes.CreateCookbookAsync(new CookbookDraft("Quick"), default);
        await recipes.AddToCookbookAsync(winter.Id, new CookbookRecipesRequest([soup.Id, stew.Id]), default);
        await recipes.AddToCookbookAsync(quick.Id, new CookbookRecipesRequest([soup.Id]), default);

        await recipes.RemoveCookbookAsync(winter.Id, default);
        await recipes.RemoveCookbookAsync(winter.Id, default);
        var soupNow = await recipes.GetAsync(soup.Id, default);

        await Assert.That(core.Recipes.Rows.Count).IsEqualTo(3);
        await Assert.That(soupNow.Cookbooks!.Single().Name).IsEqualTo("Quick");
        await Assert.That(core.Recipes.Rows.Single(row => row.Id == stew.Id).CookbookIds).IsEmpty();
        await Assert.That(core.Cookbooks.Rows.Single().Id).IsEqualTo(quick.Id);
        await Assert.That(core.Messages.Published.OfType<CookbookRemoved>().Single().CookbookId).IsEqualTo(winter.Id);
    }

    [Test]
    public async Task The_library_is_searched_and_filtered_by_cookbook()
    {
        var (core, soup, stew, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var winter = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);
        await recipes.AddToCookbookAsync(winter.Id, new CookbookRecipesRequest([soup.Id, stew.Id]), default);

        var page = await recipes.SearchAsync(new RecipeSearchRequest(null, null, 1, 25, Cookbook: winter.Id), default);
        var filters = await recipes.GetFiltersAsync(default);

        await Assert.That(string.Join(", ", page.Recipes.Select(recipe => recipe.Name))).IsEqualTo("Soup, Stew");
        await Assert.That(page.LibrarySize).IsEqualTo(3);
        await Assert.That(page.Recipes[0].Cookbooks!.Single().Name).IsEqualTo("Winter");
        await Assert.That(filters.Cookbooks.Single()).IsEqualTo(new CookbookResponse(winter.Id, "Winter", null, 2));
    }

    [Test]
    public async Task A_cookbook_downloads_as_one_file_of_only_its_recipes_named_after_it()
    {
        var (core, soup, stew, salad) = WithRecipes();
        var recipes = core.RecipeManager();
        var winter = await recipes.CreateCookbookAsync(new CookbookDraft("Grandma's Winter"), default);
        await recipes.AddToCookbookAsync(winter.Id, new CookbookRecipesRequest([soup.Id, stew.Id]), default);
        await recipes.ArchiveAsync(stew.Id, default);

        var file = await recipes.ExportCookbookAsync(winter.Id, RecipeExportFormat.Text, default);
        var text = Encoding.UTF8.GetString(file.Data);

        await Assert.That(file.FileName).IsEqualTo("grandma-s-winter.txt");
        await Assert.That(text).Contains(soup.Name);
        await Assert.That(text.Contains(stew.Name) || text.Contains(salad.Name)).IsFalse();
    }

    [Test]
    public async Task Recipes_imported_from_a_file_or_a_web_page_go_in_the_cookbook_chosen()
    {
        var (core, _, _, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var imported = await recipes.CreateCookbookAsync(new CookbookDraft("Imported"), default);
        const string file = """{"@context": "https://schema.org", "@type": "Recipe", "name": "Banana Bread", "recipeIngredient": ["3 bananas"]}""";

        var result = await recipes.ImportFileAsync(new ImportFileRequest("bread.json", file, imported.Id), default);
        var bread = core.Recipes.Rows.Single(row => row.Name == "Banana Bread");

        await Assert.That(result.Added).IsEqualTo(1);
        await Assert.That(bread.CookbookIds).IsEquivalentTo([imported.Id]);
        await Assert.That(async () => await recipes.ImportFileAsync(new ImportFileRequest("bread.json", file, Guid.NewGuid()), default))
            .Throws<NotFoundException>();
    }

    [Test]
    public async Task Being_in_a_cookbook_or_none_is_never_a_health_issue()
    {
        var bare = TestData.NewRecipe("Mystery", RecipeCourse.Main, false, "zorblax fruit");
        var filed = TestData.NewRecipe("Mystery", RecipeCourse.Main, false, "zorblax fruit");
        filed.CookbookIds = [Guid.NewGuid()];

        await Assert.That(RecipeHealth.Issues(filed)).IsEquivalentTo(RecipeHealth.Issues(bare));
        await Assert.That(RecipeHealth.Issues(filed)).Contains(RecipeIssue.NoTags);
    }

    [Test]
    public async Task A_health_report_for_a_cookbook_looks_only_at_its_recipes()
    {
        var (core, soup, stew, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var winter = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);
        await recipes.AddToCookbookAsync(winter.Id, new CookbookRecipesRequest([soup.Id, stew.Id]), default);

        var whole = await recipes.GetHealthAsync(null, default);
        var scoped = await recipes.GetHealthAsync(winter.Id, default);

        await Assert.That(whole.Recipes).IsEqualTo(3);
        await Assert.That(whole.Cookbook).IsNull();
        await Assert.That(scoped.Recipes).IsEqualTo(2);
        await Assert.That(scoped.Cookbook).IsEqualTo(new RecipeCookbookResponse(winter.Id, "Winter"));
        await Assert.That(scoped.Checks.SelectMany(check => check.Recipes).All(recipe => recipe.Id == soup.Id || recipe.Id == stew.Id)).IsTrue();
        await Assert.That(async () => await recipes.GetHealthAsync(Guid.NewGuid(), default)).Throws<NotFoundException>();
    }

    [Test]
    public async Task A_cookbook_rule_plans_its_recipes_and_is_described_by_name()
    {
        var winter = Guid.NewGuid();
        var soup = TestData.NewRecipe("Winter Soup", RecipeCourse.Main, false, "2 cups broth");
        soup.CookbookIds = [winter];
        var style = PlanStyle.Classic();
        style.Rules = [new PlanRule { Limit = RuleLimit.AtLeast, Count = 2, Slot = MealSlot.Dinner, Match = RecipeMatchKind.Cookbook, Values = [winter.ToString()] }];

        var draft = TestData.Planning.Plan(new PlanRequest(
            TestData.Sunday, [.. TestData.Library, soup], new HashSet<Guid>(), PantrySnapshot.Empty, DateTimeOffset.UnixEpoch,
            Style: style, CookbookNames: new Dictionary<Guid, string> { [winter] = "Winter" }));

        await Assert.That(TestData.Dinners(draft.Plan).Select(dinner => dinner.RecipeId)).Contains(soup.Id);
        await Assert.That(draft.Warnings).Contains("Only 1 of 2 dinners could be in the Winter cookbook, as there aren't enough such recipes. Add some, or change the plan style.");
        await Assert.That(RecipeMatcher.Describe(style.Rules[0])).IsEqualTo("in a cookbook that has been removed");
    }

    [Test]
    public async Task Cookbook_rules_must_name_a_cookbook_that_exists()
    {
        var (core, _, _, _) = WithRecipes();
        var winter = await core.RecipeManager().CreateCookbookAsync(new CookbookDraft("Winter"), default);
        var plans = core.PlanManager();

        var none = await Assert.That(async () => await plans.SaveStylesAsync(new SavePlanStylesRequest([Style()]), default))
            .Throws<RuleViolationException>();
        var gone = await Assert.That(async () => await plans.SaveStylesAsync(new SavePlanStylesRequest([Style(Guid.NewGuid())]), default))
            .Throws<NotFoundException>();
        var saved = await plans.SaveStylesAsync(new SavePlanStylesRequest([Style(winter.Id)]), default);

        await Assert.That(none!.Message).IsEqualTo("Winter dinners has a cookbook rule without a cookbook. Choose one, or remove the rule.");
        await Assert.That(gone!.Message).IsEqualTo("A cookbook rule names a cookbook that no longer exists. Reload the page and choose another.");
        await Assert.That(saved.Styles.Single().Rules!.Single().Values).IsEquivalentTo([winter.Id.ToString()]);
        await Assert.That(saved.Cookbooks!.Single().Name).IsEqualTo("Winter");
    }

    [Test]
    public async Task Removing_a_cookbook_takes_it_out_of_plan_style_rules_and_drops_rules_left_empty()
    {
        var (core, _, _, _) = WithRecipes();
        var recipes = core.RecipeManager();
        var winter = await recipes.CreateCookbookAsync(new CookbookDraft("Winter"), default);
        var quick = await recipes.CreateCookbookAsync(new CookbookDraft("Quick"), default);
        var onlyWinter = new PlanRuleDetails(RuleLimit.AtLeast, 1, MealSlot.Dinner, RecipeMatchKind.Cookbook, [winter.Id.ToString()]);
        var both = new PlanRuleDetails(RuleLimit.AtMost, 2, MealSlot.Dinner, RecipeMatchKind.Cookbook, [winter.Id.ToString(), quick.Id.ToString()]);
        await core.PlanManager().SaveStylesAsync(new SavePlanStylesRequest([Style() with { Rules = [onlyWinter, both] }]), default);

        await recipes.RemoveCookbookAsync(winter.Id, default);
        await core.HandleEventsAsync();
        var again = await core.PlanManager().ForgetCookbookAsync(winter.Id, default);

        var rule = core.Settings.Styles.Single().Rules.Single();
        await Assert.That(rule.Limit).IsEqualTo(RuleLimit.AtMost);
        await Assert.That(rule.Values).IsEquivalentTo([quick.Id.ToString()]);
        await Assert.That(again).IsEqualTo(0);
    }

    /// <summary>A style of four dinners with a cookbook rule naming the cookbooks given.</summary>
    private static PlanStyleDetails Style(params Guid[] cookbooks) => new(
        null, "Winter dinners", string.Empty, true, [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday], false, 0, 0, 0, 0,
        [new PlanRuleDetails(RuleLimit.AtLeast, 1, MealSlot.Dinner, RecipeMatchKind.Cookbook, [.. cookbooks.Select(id => id.ToString())])]);
}
