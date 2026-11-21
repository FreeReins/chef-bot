using ChefBot.Core.Engines;
using ChefBot.Core.Managers;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

/// <summary>Managers built on the fakes above, with the real engines.</summary>
internal sealed class TestCore
{
    public FixedClock Clock { get; } = new();

    public FakeRecipes Recipes { get; } = new();

    public FakePlans Plans { get; }

    public FakePantry Pantry { get; } = new();

    public FakeShoppingList ShoppingList { get; } = new();

    public FakeAccounts Accounts { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeSource Mealie { get; } = new();

    public FakeImageSource Images { get; } = new();

    public FakeWebPages Web { get; } = new();

    public FakeMessages Messages { get; } = new();

    public FakeKnownStores KnownStores { get; } = new();

    public TestCore(WeeklyScheduleOptions? schedule = null, PlannerOptions? options = null)
    {
        Plans = new FakePlans(Recipes);
        Options = options ?? TestData.Options;
        Schedule = schedule ?? new WeeklyScheduleOptions();
    }

    public PlannerOptions Options { get; }

    public WeeklyScheduleOptions Schedule { get; }

    private ValidationEngine Validation => new(Plans, Recipes, Pantry, ShoppingList, Accounts, new NoBackups());

    public PlanManager PlanManager() => new(
        Plans, Recipes, Pantry, ShoppingList, Settings, KnownStores, Messages,
        new PlanningEngine(TestData.Catalog, Options, Schedule), TestData.Shopping, TestData.Suggestions, TestData.Transformation,
        Validation, TestData.Catalog, Options, new PlanWriteLock(), Clock);

    public PantryManager PantryManager() => new(Pantry, Messages, TestData.Transformation, Validation, TestData.Catalog, Options, Clock);

    public RecipeManager RecipeManager() => new(Recipes, Settings, Mealie, Images, Web, new NoSamples(), Messages, TestData.Transformation, Validation, Clock);

    public AccountManager AccountManager() => new(Accounts, Settings, Messages, Validation, new SecurityEngine(), new PlainHasher(), Clock);
}
