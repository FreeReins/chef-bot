using ChefBot.Core.Engines;
using ChefBot.Core.Managers;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using ChefBot.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace ChefBot.Core.Tests;

/// <summary>
/// Managers built on the fakes above, with the real engines. They are made by dependency injection, so a manager
/// that takes a new accessor or engine needs only that registered here, not a change to every call.
/// </summary>
internal sealed class TestCore
{
    private readonly Lazy<ServiceProvider> services;

    public TestCore(WeeklyScheduleOptions? schedule = null, PlannerOptions? options = null)
    {
        Plans = new FakePlans(Recipes);
        Favourites = new FakeFavourites(Accounts);
        Options = options ?? TestData.Options;
        Schedule = schedule ?? new WeeklyScheduleOptions();
        services = new(Register);
    }

    public FixedClock Clock { get; } = new();

    public FakeRecipes Recipes { get; } = new();

    public FakePlans Plans { get; }

    public FakePantry Pantry { get; } = new();

    public FakeShoppingList ShoppingList { get; } = new();

    public FakeAccounts Accounts { get; } = new();

    public FakeHistory History { get; } = new();

    public FakeFavourites Favourites { get; }

    public FakeReviews Reviews { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeImageSource Images { get; } = new();

    public FakeWebPages Web { get; } = new();

    public FakeToDoLists ToDoLists { get; } = new();

    public CalendarOptions Calendar { get; } = new();

    public FakeMessages Messages { get; } = new();

    public FakeKnownStores KnownStores { get; } = new();

    public FakeKnownPlanStyles KnownStyles { get; } = new();

    public PlannerOptions Options { get; }

    public WeeklyScheduleOptions Schedule { get; }

    public PlanManager PlanManager() => Make<PlanManager>();

    public PantryManager PantryManager() => Make<PantryManager>();

    public RecipeManager RecipeManager() => Make<RecipeManager>();

    public AccountManager AccountManager() => Make<AccountManager>();

    /// <summary>A new manager each call, as each request gets in the app; the fakes and engines are shared.</summary>
    private T Make<T>()
        where T : notnull => services.Value.GetRequiredService<T>();

    private ServiceProvider Register()
    {
        var services = new ServiceCollection();

        // Fakes for every accessor.
        services.AddSingleton<IRecipeAccess>(Recipes);
        services.AddSingleton<IPlanAccess>(Plans);
        services.AddSingleton<IPantryAccess>(Pantry);
        services.AddSingleton<IShoppingListAccess>(ShoppingList);
        services.AddSingleton<IAccountAccess>(Accounts);
        services.AddSingleton<IHistoryAccess>(History);
        services.AddSingleton<IFavouriteAccess>(Favourites);
        services.AddSingleton<IReviewAccess>(Reviews);
        services.AddSingleton<ISettingsAccess>(Settings);
        services.AddSingleton<IImageSourceAccess>(Images);
        services.AddSingleton<IWebPageAccess>(Web);
        services.AddSingleton<IToDoListAccess>(ToDoLists);
        services.AddSingleton<IMessageAccess>(Messages);
        services.AddSingleton<IKnownStoreAccess>(KnownStores);
        services.AddSingleton<IKnownPlanStyleAccess>(KnownStyles);
        services.AddSingleton<ISampleRecipeAccess>(new NoSamples());
        services.AddSingleton<IBackupAccess>(new NoBackups());

        // Settings, utilities and the real engines.
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(Options);
        services.AddSingleton(Schedule);
        services.AddSingleton(Calendar);
        services.AddSingleton(TestData.Catalog);
        services.AddSingleton<IPasswordHasher>(new PlainHasher());
        services.AddTransient<PlanWriteLock>();
        services.AddSingleton(TestData.Transformation);
        services.AddSingleton(TestData.Shopping);
        services.AddSingleton(TestData.Suggestions);
        services.AddSingleton<PlanningEngine>();
        services.AddSingleton<CalendarEngine>();
        services.AddSingleton<HistoryEngine>();
        services.AddSingleton<AchievementEngine>();
        services.AddSingleton<SecurityEngine>();
        services.AddTransient<ValidationEngine>();

        // The managers under test.
        services.AddTransient<PlanManager>();
        services.AddTransient<PantryManager>();
        services.AddTransient<RecipeManager>();
        services.AddTransient<AccountManager>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
