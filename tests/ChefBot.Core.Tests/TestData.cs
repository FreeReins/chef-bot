using ChefBot.Core.Ingredients;
using ChefBot.Core.Pantry;
using ChefBot.Core.Planning;
using ChefBot.Core.Recipes;
using ChefBot.Core.Shopping;

namespace ChefBot.Core.Tests;

/// <summary>A small catalog and recipe library shared by the tests.</summary>
internal static class TestData
{
    /// <summary>A Sunday in November: cabbage, kale and carrots are in season, tomatoes and basil are not.</summary>
    public static readonly DateOnly Sunday = new(2026, 11, 8);

    /// <summary>A Sunday in August: tomatoes and basil are in season, cabbage and kale are not.</summary>
    public static readonly DateOnly SummerSunday = new(2026, 8, 2);

    public static readonly IngredientCatalog Catalog = new(
    [
        Entry("cabbage", IngredientGroup.Vegetable, 14, season: [6, 7, 9, 10, 11, 12, 1, 2]),
        Entry("kale", IngredientGroup.Vegetable, 7, season: [5, 6, 9, 10, 11]),
        Entry("carrot", IngredientGroup.Vegetable, 28, season: [6, 7, 9, 10, 11, 12, 1, 2]),
        Entry("tomato", IngredientGroup.Vegetable, 7, season: [7, 8, 9]),
        Entry("winter squash", IngredientGroup.Vegetable, 60, aliases: ["butternut squash"], keepSpecific: true),
        Entry("apple", IngredientGroup.Fruit, 30, season: [8, 9, 10, 11, 12, 1, 2]),
        Entry("cilantro", IngredientGroup.Herb, 7, season: [5, 6, 9, 10]),
        Entry("basil", IngredientGroup.Herb, 5, season: [6, 7, 8, 9]),
        Entry("green onion", IngredientGroup.Herb, 10, aliases: ["scallion"]),
        Entry("chicken", IngredientGroup.Protein, 2, aliases: ["chicken thigh", "chicken breast"]),
        Entry("beef", IngredientGroup.Protein, 2, aliases: ["stew meat"]),
        Entry("white fish", IngredientGroup.Protein, 2, aliases: ["cod"]),
        Entry("egg", IngredientGroup.Protein, 28),
        Entry("brown rice", IngredientGroup.WholeGrain, 180),
        Entry("quinoa", IngredientGroup.WholeGrain, 365),
        Entry("tortilla", IngredientGroup.Other, 14),
        Entry("oats", IngredientGroup.Other, 365),
        Entry("canned tomatoes", IngredientGroup.Other, 730, aliases: ["diced tomatoes"]),
        Entry("chicken broth", IngredientGroup.Other, 365),
        Entry("salt", IngredientGroup.Staple, null, staple: true),
    ]);

    public static readonly PlannerOptions Options = new();

    public static readonly Recipe Stew = NewRecipe("Slow Beef Stew", RecipeCourse.Main, true, "2 lb stew meat", "3 carrots", "1 tsp salt");
    public static readonly Recipe Tacos = NewRecipe("Slaw Fish Tacos", RecipeCourse.Main, false, "0.5 head cabbage", "1 bunch cilantro", "1 lb cod", "8 tortillas");
    public static readonly Recipe Bowls = NewRecipe("Chicken Cabbage Bowls", RecipeCourse.Main, false, "0.5 head cabbage", "0.5 bunch cilantro", "1 lb chicken thigh", "2 cups brown rice");
    public static readonly Recipe Soup = NewRecipe("Kale Soup", RecipeCourse.Main, false, "1 bunch kale", "4 cups chicken broth");
    public static readonly Recipe Caprese = NewRecipe("Summer Caprese", RecipeCourse.Main, false, "4 tomatoes", "1 bunch basil");
    public static readonly Recipe TomatoSalad = NewRecipe("Tomato Basil Salad", RecipeCourse.Main, false, "2 tomatoes", "1 bunch basil");

    public static readonly IReadOnlyList<Recipe> Library =
    [
        Stew, Tacos, Bowls, Soup, Caprese, TomatoSalad,
        NewRecipe("Oatmeal", RecipeCourse.Breakfast, false, "2 cups oats", "2 apples"),
        NewRecipe("Egg Scramble", RecipeCourse.Breakfast, false, "6 eggs", "0.5 bunch kale"),
        NewRecipe("Apple Slices", RecipeCourse.Snack, false, "2 apples"),
        NewRecipe("Carrot Sticks", RecipeCourse.Snack, false, "3 carrots"),
        NewRecipe("Kale Chips", RecipeCourse.Snack, false, "1 bunch kale"),
        NewRecipe("Baked Apples", RecipeCourse.Sweet, false, "4 apples"),
    ];

    public static Recipe NewRecipe(string name, RecipeCourse course, bool slowCooker, params string[] ingredients)
    {
        var recipe = new Recipe { Name = name, Course = course, IsSlowCooker = slowCooker, Servings = 4 };
        recipe.Ingredients.AddRange(new RecipeIngredientBuilder(Catalog).FromLines(ingredients));
        return recipe;
    }

    public static PlanDraft PlanWeek(DateOnly weekStart, PantrySnapshot? pantry = null, IReadOnlyList<Recipe>? recipes = null) =>
        new WeekPlanner(Catalog, Options).Plan(new PlanRequest(
            weekStart, recipes ?? Library, new HashSet<Guid>(), pantry ?? PantrySnapshot.Empty, DateTimeOffset.UnixEpoch));

    public static ShoppingListBuilder ShoppingBuilder() =>
        new(Catalog, new StoreRouter(new StoreOptions { Default = "Store" }, Catalog));

    /// <summary>A pantry item bought on <see cref="Sunday"/>.</summary>
    public static PantryItem Stocked(string name, decimal? quantity = null, string unit = "", DateOnly? expiresOn = null) =>
        new PantryItemFactory(Catalog).Create(Catalog.Identify(name), quantity, unit, Sunday, expiresOn);

    public static PantrySnapshot PantryOf(params PantryItem[] items) => new(items, Sunday, Options.ExpiringSoonDays);

    public static List<PlannedMeal> Dinners(MealPlan plan) =>
        plan.Meals.Where(meal => meal.Slot == MealSlot.Dinner).OrderBy(meal => meal.Date).ToList();

    /// <summary>An in-memory stand-in for the stored settings.</summary>
    public sealed class FakeSettings : Abstractions.ISettingsStore
    {
        private readonly Dictionary<Type, object> _rows = new();

        public Task<TSettings> GetAsync<TSettings>(CancellationToken cancellationToken)
            where TSettings : class, new()
        {
            if (!_rows.TryGetValue(typeof(TSettings), out var row))
            {
                row = new TSettings();
                _rows[typeof(TSettings)] = row;
            }

            return Task.FromResult((TSettings)row);
        }
    }

    private static CatalogEntry Entry(
        string name, IngredientGroup group, int? shelfLife,
        int[]? season = null, string[]? aliases = null, bool staple = false, bool keepSpecific = false) => new()
    {
        Name = name,
        Group = group,
        ShelfLifeDays = shelfLife,
        SeasonMonths = season ?? [],
        Aliases = aliases ?? [],
        PantryStaple = staple,
        KeepSpecificName = keepSpecific,
    };
}
