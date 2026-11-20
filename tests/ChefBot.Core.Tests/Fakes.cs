using System.Text.Json;
using ChefBot.Core.Contracts;
using ChefBot.Core.Engines;
using ChefBot.Core.Managers;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

// In-memory stand-ins for the resource accessors, so managers can be tested with their real engines and no
// database. Like the real accessors they hand out copies: a change only counts once it is saved.

internal static class Copies
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = false };

    public static T Of<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!;
}

internal sealed class FixedClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<object> Published { get; } = [];

    public int Commits { get; private set; }

    public Task PublishAsync(object message)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Commits++;
        return Task.CompletedTask;
    }
}

internal sealed class PlainHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password.Length}:{password.GetHashCode()}";

    public bool Verify(string hash, string password) => hash == Hash(password);
}

internal sealed class FakeRecipes : IRecipeAccess
{
    public List<Recipe> Rows { get; } = [];

    public Task<List<Recipe>> ListActiveAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(r => !r.IsArchived).OrderBy(r => r.Name).Select(Copies.Of).ToList());

    public Task<List<Recipe>> ListAllAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task<Recipe?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(r => r.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(r => r.Id == recipe.Id);
        Rows.Add(Copies.Of(recipe));
        return Task.CompletedTask;
    }
}

internal sealed class FakePlans(FakeRecipes recipes) : IPlanAccess
{
    public List<MealPlan> Rows { get; } = [];

    public Task<MealPlan?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Load(Rows.FirstOrDefault(p => p.Id == id)));

    public Task<MealPlan?> FindByWeekAsync(DateOnly weekStart, CancellationToken cancellationToken) =>
        Task.FromResult(Load(Rows.FirstOrDefault(p => p.WeekStart == weekStart)));

    public Task<MealPlan?> FindCurrentAsync(DateOnly today, CancellationToken cancellationToken) =>
        Task.FromResult(Load(
            Rows.Where(p => p.WeekStart >= today.AddDays(-6) && p.WeekStart <= today).MaxBy(p => p.WeekStart)
            ?? Rows.Where(p => p.WeekStart > today).MinBy(p => p.WeekStart)));

    public Task<List<MealPlan>> ListOpenAsync(DateOnly today, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(p => p.WeekStart >= today.AddDays(-6)).Select(p => Load(p)!).ToList());

    public Task<HashSet<Guid>> RecipeIdsPlannedAsync(DateOnly from, DateOnly before, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.Where(p => p.WeekStart >= from && p.WeekStart < before)
            .SelectMany(p => p.Meals).Where(m => m.RecipeId is not null).Select(m => m.RecipeId!.Value).ToHashSet());

    public Task SaveAsync(MealPlan plan, CancellationToken cancellationToken)
    {
        if (Rows.Any(p => p.WeekStart == plan.WeekStart && p.Id != plan.Id))
        {
            throw new InvalidOperationException("Two plans for one week: the database's unique index would refuse this.");
        }

        var stored = Copies.Of(plan);
        stored.Meals.ForEach(meal => meal.Recipe = null); // Only the recipe id is stored.
        Rows.RemoveAll(p => p.Id == plan.Id);
        Rows.Add(stored);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }

    /// <summary>A copy of a stored plan with each meal's recipe loaded, as the database query does.</summary>
    private MealPlan? Load(MealPlan? row)
    {
        if (row is null)
        {
            return null;
        }

        var plan = Copies.Of(row);
        foreach (var meal in plan.Meals.Where(m => m.RecipeId is not null))
        {
            meal.Recipe = recipes.Rows.FirstOrDefault(r => r.Id == meal.RecipeId) is { } recipe ? Copies.Of(recipe) : null;
        }

        return plan;
    }
}

internal sealed class FakePantry : IPantryAccess
{
    public List<PantryItem> Rows { get; } = [];

    public Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(i => i.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(PantryItem item, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(i => i.Id == item.Id);
        Rows.Add(Copies.Of(item));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(i => i.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAccounts : IAccountAccess
{
    public List<UserAccount> Users { get; } = [];

    public List<AccessLink> Links { get; } = [];

    public UserAccount User(string username) => Users.Single(u => u.Username == username);

    public Task<bool> AnyUsersAsync(CancellationToken cancellationToken) => Task.FromResult(Users.Count > 0);

    public Task<UserAccount?> FindUserAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id) is { } row ? Copies.Of(row) : null);

    public Task<UserAccount?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Username == username) is { } row ? Copies.Of(row) : null);

    public Task<List<UserAccount>> ListUsersAsync(CancellationToken cancellationToken) => Task.FromResult(Users.Select(Copies.Of).ToList());

    public Task SaveUserAsync(UserAccount user, CancellationToken cancellationToken)
    {
        Users.RemoveAll(u => u.Id == user.Id);
        Users.Add(Copies.Of(user));
        return Task.CompletedTask;
    }

    public Task RemoveUserAsync(Guid id, CancellationToken cancellationToken)
    {
        Users.RemoveAll(u => u.Id == id);
        return Task.CompletedTask;
    }

    public Task<AccessLink?> FindLinkAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Links.FirstOrDefault(l => l.Id == id) is { } row ? Copies.Of(row) : null);

    public Task<AccessLink?> FindLinkByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Links.FirstOrDefault(l => l.TokenHash == tokenHash) is { } row ? Copies.Of(row) : null);

    public Task<List<AccessLink>> ListLinksAsync(CancellationToken cancellationToken) => Task.FromResult(Links.Select(Copies.Of).ToList());

    public Task SaveLinkAsync(AccessLink link, CancellationToken cancellationToken)
    {
        Links.RemoveAll(l => l.Id == link.Id);
        Links.Add(Copies.Of(link));
        return Task.CompletedTask;
    }

    public Task RemoveLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        Links.RemoveAll(l => l.Id == id);
        return Task.CompletedTask;
    }
}

internal sealed class FakeSettings : ISettingsAccess
{
    public SecuritySettings Security { get; set; } = new();

    public NotificationSettings Notifications { get; set; } = new();

    public BackupSettings Backups { get; set; } = new();

    public MealieSettings Mealie { get; set; } = new();

    public ScheduleState Schedule { get; set; } = new();

    public Task<SecuritySettings> GetSecurityAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Security));

    public Task SaveSecurityAsync(SecuritySettings settings, CancellationToken cancellationToken) => Set(() => Security = Copies.Of(settings));

    public Task<NotificationSettings> GetNotificationsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Notifications));

    public Task SaveNotificationsAsync(NotificationSettings settings, CancellationToken cancellationToken) => Set(() => Notifications = Copies.Of(settings));

    public Task<BackupSettings> GetBackupsAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Backups));

    public Task SaveBackupsAsync(BackupSettings settings, CancellationToken cancellationToken) => Set(() => Backups = Copies.Of(settings));

    public Task<MealieSettings> GetMealieAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Mealie));

    public Task SaveMealieAsync(MealieSettings settings, CancellationToken cancellationToken) => Set(() => Mealie = Copies.Of(settings));

    public Task<ScheduleState> GetScheduleAsync(CancellationToken cancellationToken) => Task.FromResult(Copies.Of(Schedule));

    public Task SaveScheduleAsync(ScheduleState state, CancellationToken cancellationToken) => Set(() => Schedule = Copies.Of(state));

    private static Task Set(Action save)
    {
        save();
        return Task.CompletedTask;
    }
}

internal sealed class NoBackups : IBackupAccess
{
    public Task<StoredBackup> CreateAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();

    public IReadOnlyList<StoredBackup> List() => [];

    public void Delete(string name) => throw new NotSupportedException();

    public Stream OpenRead(string name) => throw new NotSupportedException();

    public Task RestoreAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
}

/// <summary>A Mealie that returns the recipes it is given, or fails when it has none, and records the connection used.</summary>
internal sealed class FakeSource : IRecipeSourceAccess
{
    public List<SourceRecipe>? Recipes { get; set; }

    public string? UsedUrl { get; private set; }

    public string? UsedToken { get; private set; }

    public Task<RecipeSourceResult> FetchAllAsync(MealieSettings connection, CancellationToken cancellationToken)
    {
        UsedUrl = connection.Url;
        UsedToken = connection.ApiToken;
        return Recipes is null
            ? throw new RecipeSourceException("Not a real Mealie.")
            : Task.FromResult(new RecipeSourceResult(Recipes, 0));
    }
}

internal sealed class NoSamples : ISampleRecipeAccess
{
    public IReadOnlyList<RecipeText> All() => [];
}

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
