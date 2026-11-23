using ChefBot.Core.Engines;
using ChefBot.Core.Managers;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
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

    public TestCore(PlannerOptions? options = null)
    {
        Plans = new FakePlans(Recipes);
        Favourites = new FakeFavourites(Accounts);
        Recipes.Favourites = () => Favourites.Rows;
        Recipes.Suggestions = () => Suggestions.Rows;
        Options = options ?? TestData.Options;
        services = new(Register);
    }

    public FixedClock Clock { get; } = new();

    public FakeRecipes Recipes { get; } = new();

    public FakePlans Plans { get; }

    public FakePantry Pantry { get; } = new();

    public FakeShoppingList ShoppingList { get; } = new();

    public FakeAccounts Accounts { get; } = new();

    public FakeHistory History { get; } = new();

    public FakeInbox Inbox { get; } = new();

    public FakeAchievements Achievements { get; } = new();

    public FakeWebhook Webhook { get; } = new();

    private int handled;

    public FakeFavourites Favourites { get; }

    public FakeReviews Reviews { get; } = new();

    public FakeSettings Settings { get; } = new();

    public FakeImageSource Images { get; } = new();

    public FakeWebPages Web { get; } = new();

    public FakeToDoLists ToDoLists { get; } = new();

    public FakeAi Ai { get; } = new();

    public FakeRecipeSuggestions Suggestions { get; } = new();

    public FakeCookbooks Cookbooks { get; } = new();

    public FakeMessages Messages { get; } = new();

    public FakeKnownStores KnownStores { get; } = new();

    public FakeKnownPlanStyles KnownStyles { get; } = new();

    public PlannerOptions Options { get; }

    public PlanManager PlanManager() => Make<PlanManager>();

    public PantryManager PantryManager() => Make<PantryManager>();

    public RecipeManager RecipeManager() => Make<RecipeManager>();

    public AccountManager AccountManager() => Make<AccountManager>();

    public MaintenanceManager MaintenanceManager() => Make<MaintenanceManager>();

    /// <summary>
    /// Does what the background handlers do in the app with the events published since the last call: writes the
    /// food history, brings the open plans in line after a pantry, store or settings change, a plan's shopping list
    /// in line after a recipe is added to it, and the plan styles in line after a cookbook is removed.
    /// </summary>
    public async Task HandleEventsAsync()
    {
        while (handled < Messages.Published.Count)
        {
            switch (Messages.Published[handled++])
            {
                case IKitchenMoment moment:
                    await RecipeManager().RecordHistoryAsync(moment, Guid.NewGuid(), default);
                    break;
                case PantryChanged or GroceryStoresChanged or PlanningSettingsChanged:
                    await PlanManager().RefreshOpenPlansAsync(default);
                    break;
                case MealAdded added:
                    await PlanManager().CatchUpAsync(added.PlanId, default);
                    break;
                case CookbookRemoved removed:
                    await PlanManager().ForgetCookbookAsync(removed.CookbookId, default);
                    break;
            }
        }
    }

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
        services.AddSingleton<IInboxAccess>(Inbox);
        services.AddSingleton<IAchievementAccess>(Achievements);
        services.AddSingleton<INotificationAccess>(Webhook);
        services.AddSingleton<IFavouriteAccess>(Favourites);
        services.AddSingleton<IReviewAccess>(Reviews);
        services.AddSingleton<ISettingsAccess>(Settings);
        services.AddSingleton<IImageSourceAccess>(Images);
        services.AddSingleton<IWebPageAccess>(Web);
        services.AddSingleton<IToDoListAccess>(ToDoLists);
        services.AddSingleton<IAiAccess>(Ai);
        services.AddSingleton<IRecipeSuggestionAccess>(Suggestions);
        services.AddSingleton<ICookbookAccess>(Cookbooks);
        services.AddSingleton<IMessageAccess>(Messages);
        services.AddSingleton<IKnownStoreAccess>(KnownStores);
        services.AddSingleton<IKnownPlanStyleAccess>(KnownStyles);
        services.AddSingleton<ISampleRecipeAccess>(new NoSamples());
        services.AddSingleton<IBackupAccess>(new NoBackups());

        // Settings, utilities and the real engines.
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton(Options);
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
        services.AddSingleton(TestData.Notifications);
        services.AddSingleton<SecurityEngine>();
        services.AddTransient<ValidationEngine>();

        // The managers under test.
        services.AddTransient<PlanManager>();
        services.AddTransient<PantryManager>();
        services.AddTransient<MaintenanceManager>();
        services.AddTransient<RecipeManager>();
        services.AddTransient<AccountManager>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
