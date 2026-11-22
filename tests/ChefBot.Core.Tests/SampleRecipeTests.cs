using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Transformation;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

/// <summary>
/// The starter recipes in config/sample-recipes.json, read with the real ingredient catalog: each is complete enough
/// to cook from, and together they can fill a week in every starter plan style.
/// </summary>
public class SampleRecipeTests
{
    private static readonly string Config = Path.Combine(AppContext.BaseDirectory, "config");

    private static readonly IngredientCatalog Catalog = IngredientCatalog.FromJson(File.ReadAllText(Path.Combine(Config, "ingredients.json")));

    private static readonly IReadOnlyList<Recipe> Samples = Load();

    private static readonly IReadOnlyList<PlanStyle> Styles = KnownPlanStyleAccess.FromJson(File.ReadAllText(Path.Combine(Config, "plan-styles.json")));

    [Test]
    public async Task Every_sample_has_nothing_left_to_fill_in()
    {
        var gaps = Samples
            .SelectMany(recipe => RecipeCompleteness.Gaps(recipe).Select(gap => $"{recipe.Name}: {gap}"))
            .ToList();

        await Assert.That(Samples.Count).IsGreaterThan(0);
        await Assert.That(gaps).IsEmpty();
    }

    [Test]
    public async Task Sample_names_are_unique()
    {
        var names = Samples.Select(recipe => recipe.Name).ToList();

        await Assert.That(names.Distinct(StringComparer.OrdinalIgnoreCase).Count()).IsEqualTo(names.Count);
    }

    [Test]
    public async Task Every_ingredient_with_an_amount_names_a_food_not_a_size()
    {
        // "1 inch ginger" or "1 large onion" leave a size at the front of the name when the catalog doesn't know the food.
        string[] sizes = ["inch", "large", "medium", "small"];
        var odd = Samples
            .SelectMany(recipe => recipe.Ingredients.Select(ingredient => (recipe.Name, ingredient)))
            .Where(pair => pair.ingredient.CatalogName is null && sizes.Contains(pair.ingredient.Key.Split(' ')[0]))
            .Select(pair => $"{pair.Name}: {pair.ingredient.Text}")
            .ToList();

        await Assert.That(odd).IsEmpty();
    }

    [Test]
    public async Task Every_starter_style_can_fill_a_week_from_the_samples()
    {
        var problems = new List<string>();
        foreach (var style in Styles)
        {
            // Dinners must pass every "none" rule, and each "at least" rule needs enough of them that also do.
            var dinners = Samples
                .Where(recipe => recipe.Course == RecipeCourse.Main)
                .Where(recipe => style.Rules.Where(rule => rule.Limit == RuleLimit.None).All(rule => !RecipeMatcher.Matches(recipe, rule)))
                .ToList();
            if (dinners.Count < style.CookingDays.Count)
            {
                problems.Add($"{style.Name}: {dinners.Count} dinners for {style.CookingDays.Count} cooking days");
            }

            foreach (var rule in style.Rules.Where(rule => rule.Limit == RuleLimit.AtLeast))
            {
                var matching = dinners.Count(recipe => RecipeMatcher.Matches(recipe, rule));
                if (matching < rule.Count)
                {
                    problems.Add($"{style.Name}: {matching} dinners are {RecipeMatcher.Describe(rule)}, it needs {rule.Count}");
                }
            }

            Check(style.Name, RecipeCourse.Breakfast, style.Breakfasts, problems);
            Check(style.Name, RecipeCourse.Snack, style.Snacks, problems);
            Check(style.Name, RecipeCourse.Sweet, style.Sweets, problems);
            Check(style.Name, RecipeCourse.Drink, style.Drinks, problems);
        }

        await Assert.That(Styles.Count).IsGreaterThan(0);
        await Assert.That(problems).IsEmpty();
    }

    [Test]
    public async Task Each_broth_keeps_its_own_name()
    {
        var vegetable = Catalog.Identify("vegetable broth");
        var chicken = Catalog.Identify("low-sodium chicken broth");

        await Assert.That(vegetable.Key).IsEqualTo("vegetable broth");
        await Assert.That(vegetable.DisplayName).IsEqualTo("vegetable broth");
        await Assert.That(vegetable.CatalogName).IsEqualTo("chicken broth");
        await Assert.That(chicken.Key).IsEqualTo("chicken broth");
        await Assert.That(Catalog.Identify("beef stock").Key).IsEqualTo("beef stock");
    }

    [Test]
    public async Task Vegetable_broth_is_not_chicken_but_chicken_broth_is()
    {
        var chicken = new PlanRule { Limit = RuleLimit.None, Match = RecipeMatchKind.Ingredient, Values = ["chicken"] };
        var curry = Samples.Single(recipe => recipe.Name == "Butternut Squash Lentil Curry");
        var soup = Samples.Single(recipe => recipe.Name == "Kale, Sausage and White Bean Soup");

        await Assert.That(RecipeMatcher.Matches(curry, chicken)).IsFalse();
        await Assert.That(RecipeMatcher.Matches(soup, chicken)).IsTrue();
    }

    [Test]
    public async Task Sesame_oil_is_shopped_for_not_taken_as_cooking_oil()
    {
        await Assert.That(Catalog.Find("2 tsp toasted sesame oil")?.Name).IsEqualTo("sesame oil");
        await Assert.That(Catalog.Find("2 tbsp vegetable oil")?.PantryStaple).IsTrue();
    }

    private static void Check(string style, RecipeCourse course, int wanted, List<string> problems)
    {
        var have = Samples.Count(recipe => recipe.Course == course);
        if (have < wanted)
        {
            problems.Add($"{style}: {have} {course} samples, it needs {wanted}");
        }
    }

    private static IReadOnlyList<Recipe> Load()
    {
        var transformation = new TransformationEngine(Catalog, [], []);
        return new SampleRecipeAccess(Path.Combine(Config, "sample-recipes.json")).All()
            .Select(text =>
            {
                var recipe = new Recipe();
                transformation.ApplyText(text, recipe, DateTimeOffset.UnixEpoch);
                return recipe;
            })
            .ToList();
    }
}
