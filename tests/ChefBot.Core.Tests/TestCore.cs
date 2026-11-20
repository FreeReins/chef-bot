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

    public FakeAccounts Accounts { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeSource Mealie { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public TestCore(WeeklyScheduleOptions? schedule = null, PlannerOptions? options = null)
    {
        Plans = new FakePlans(Recipes);
        Options = options ?? TestData.Options;
        Schedule = schedule ?? new WeeklyScheduleOptions();
    }

    public PlannerOptions Options { get; }

    public WeeklyScheduleOptions Schedule { get; }

    private ValidationEngine Validation => new(Plans, Recipes, Pantry, Accounts, new NoBackups());

    public PlanManager PlanManager() => new(
        Plans, Recipes, Pantry, Settings, UnitOfWork,
        new PlanningEngine(TestData.Catalog, Options, Schedule), TestData.Shopping, TestData.Suggestions, TestData.Transformation,
        Validation, TestData.Catalog, Options, new PlanWriteLock(), Clock);

    public PantryManager PantryManager() => new(Pantry, UnitOfWork, TestData.Transformation, Validation, TestData.Catalog, Options, Clock);

    public RecipeManager RecipeManager() => new(Recipes, Settings, Mealie, new NoSamples(), UnitOfWork, TestData.Transformation, Validation, Clock);

    public AccountManager AccountManager() => new(Accounts, Settings, UnitOfWork, Validation, new SecurityEngine(), new PlainHasher(), Clock);
}
