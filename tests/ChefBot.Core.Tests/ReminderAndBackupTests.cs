using System.IO.Compression;
using System.Text;
using ChefBot.Core.Engines;
using ChefBot.Core.Engines.Contracts.Messages;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.Utilities;

namespace ChefBot.Core.Tests;

public class ReminderAndBackupTests
{
    private static readonly DateOnly Tuesday = TestData.Sunday.AddDays(2);

    private static IReadOnlyList<string> TypesOn(MealPlan? plan, DateOnly day, params PantryItem[] pantry) =>
        TestData.Notifications.Daily(plan, pantry, day, expiringSoonDays: 3).Select(reminder => reminder.Type).ToList();

    [Test]
    public async Task The_slow_cooker_reminder_goes_out_on_the_morning_of_the_meal()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;

        var tuesday = TestData.Notifications.Daily(plan, [], Tuesday, 3).Single(reminder => reminder.Type == NotificationTypes.SlowCooker);

        await Assert.That(tuesday.Message.Contains(TestData.Stew.Name)).IsTrue();
        await Assert.That(TypesOn(plan, TestData.Sunday.AddDays(4)).Contains(NotificationTypes.SlowCooker)).IsFalse();
    }

    [Test]
    public async Task A_cooked_or_skipped_slow_cooker_meal_needs_no_reminder()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        TestData.Dinners(plan)[1].Status = MealStatus.Skipped;

        await Assert.That(TypesOn(plan, Tuesday).Contains(NotificationTypes.SlowCooker)).IsFalse();
    }

    [Test]
    public async Task Todays_meals_are_listed_in_order()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;

        var today = TestData.Notifications.Daily(plan, [], Tuesday, 3).Single(reminder => reminder.Type == NotificationTypes.MealsToday);

        await Assert.That(today.Message.IndexOf("Breakfast", StringComparison.Ordinal) < today.Message.IndexOf("Dinner", StringComparison.Ordinal)).IsTrue();
        await Assert.That(today.Message.Contains("Leftovers")).IsTrue();
    }

    [Test]
    public async Task Expiring_food_is_reported_and_flags_what_no_meal_uses()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var tomatoes = TestData.Stocked("tomatoes", 4, expiresOn: TestData.Sunday.AddDays(2));
        var quinoa = TestData.Stocked("quinoa", 2, "cup");

        var reminder = TestData.Notifications.Daily(plan, [tomatoes, quinoa], TestData.Sunday, 3)
            .Single(item => item.Type == NotificationTypes.ExpiringFood);

        // Quinoa keeps for a year, so only the tomatoes are mentioned; no November meal uses them.
        await Assert.That(reminder.Message.Contains("tomato")).IsTrue();
        await Assert.That(reminder.Message.Contains("quinoa")).IsFalse();
        await Assert.That(reminder.Message.Contains("swap a meal")).IsTrue();
    }

    [Test]
    public async Task The_day_before_the_week_a_staged_plan_asks_to_be_confirmed()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        var saturday = TestData.Sunday.AddDays(-1);

        var reminder = TestData.Notifications.Daily(plan, [], saturday, 3).Single();

        await Assert.That(reminder.Type).IsEqualTo(NotificationTypes.Shopping);
        await Assert.That(reminder.Title.Contains("confirming")).IsTrue();
        await Assert.That(TypesOn(plan, TestData.Sunday.AddDays(-3)).Count).IsEqualTo(0);
    }

    [Test]
    public async Task A_confirmed_plan_only_nags_while_groceries_are_left()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        plan.Status = PlanStatus.Confirmed;
        TestData.Shopping.ApplyList(plan, TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []));
        var saturday = TestData.Sunday.AddDays(-1);

        await Assert.That(TypesOn(plan, saturday).Contains(NotificationTypes.Shopping)).IsTrue();

        plan.ShoppingItems.ForEach(item => item.IsPurchased = true);
        await Assert.That(TypesOn(plan, saturday).Contains(NotificationTypes.Shopping)).IsFalse();
    }

    [Test]
    public async Task No_plan_means_no_plan_reminders()
    {
        await Assert.That(TypesOn(null, TestData.Sunday).Count).IsEqualTo(0);
    }

    [Test]
    public async Task The_plan_ready_notification_carries_the_dinners_for_custom_wording()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;

        var ready = TestData.Notifications.PlanReady(plan, "schedule");

        await Assert.That(ready.Type).IsEqualTo(NotificationTypes.PlanReady);
        await Assert.That(ready.Data["trigger"] as string).IsEqualTo("schedule");
        await Assert.That(((System.Collections.ICollection)ready.Data["dinners"]!).Count).IsEqualTo(4);
        await Assert.That(ready.Message.Contains(TestData.Stew.Name)).IsTrue();
    }

    [Test]
    public async Task Unwanted_reminder_types_can_be_switched_off()
    {
        var settings = new NotificationSettings { SlowCooker = false };

        await Assert.That(TestData.Notifications.IsWanted(settings, NotificationTypes.SlowCooker)).IsFalse();
        await Assert.That(TestData.Notifications.IsWanted(settings, NotificationTypes.PlanReady)).IsTrue();
        await Assert.That(TestData.Notifications.IsWanted(settings, NotificationTypes.Test)).IsTrue();
    }

    [Test]
    public async Task Daily_jobs_run_once_after_their_time()
    {
        var offset = TimeSpan.FromHours(-6);
        var morning = new DateTimeOffset(2026, 11, 10, 7, 0, 0, offset);
        var later = new DateTimeOffset(2026, 11, 10, 7, 45, 0, offset);
        var today = new DateOnly(2026, 11, 10);

        await Assert.That(DailyTime.IsDue("07:30", null, morning)).IsFalse();
        await Assert.That(DailyTime.IsDue("07:30", today.AddDays(-1), later)).IsTrue();
        await Assert.That(DailyTime.IsDue("07:30", today, later)).IsFalse();
    }

    [Test]
    public async Task Times_must_be_hours_and_minutes()
    {
        var refused = false;
        try
        {
            DailyTime.Parse("half past seven");
        }
        catch (RuleViolationException)
        {
            refused = true;
        }

        await Assert.That(refused).IsTrue();
        await Assert.That(DailyTime.Format(DailyTime.Parse("07:05"))).IsEqualTo("07:05");
    }

    [Test]
    public async Task Retention_keeps_the_newest_backups()
    {
        var now = new DateTimeOffset(2026, 11, 20, 3, 0, 0, TimeSpan.Zero);
        var files = Enumerable.Range(0, 5).Select(age => new StoredBackup($"backup-{age}", now.AddDays(-age), 100)).ToList();

        var expired = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 3, KeepDays = 0 }, now);

        await Assert.That(string.Join(",", expired.Select(file => file.Name))).IsEqualTo("backup-3,backup-4");
    }

    [Test]
    public async Task Retention_removes_backups_past_their_age_but_never_the_newest()
    {
        var now = new DateTimeOffset(2026, 11, 20, 3, 0, 0, TimeSpan.Zero);
        var files = new[] { 10, 20, 40 }.Select(age => new StoredBackup($"backup-{age}", now.AddDays(-age), 100)).ToList();

        var byAge = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 0, KeepDays = 15 }, now);
        var keepAll = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 0, KeepDays = 0 }, now);
        var allTooOld = TestData.Notifications.ExpiredBackups(files, new BackupSettings { KeepCount = 0, KeepDays = 5 }, now);

        await Assert.That(string.Join(",", byAge.Select(file => file.Name))).IsEqualTo("backup-20,backup-40");
        await Assert.That(keepAll.Count).IsEqualTo(0);
        await Assert.That(string.Join(",", allTooOld.Select(file => file.Name))).IsEqualTo("backup-20,backup-40");
    }

    [Test]
    public async Task Regenerating_after_shopping_builds_the_week_around_what_was_bought()
    {
        // Tomatoes and basil are out of season in November, so a normal plan avoids them...
        var pantry = TestData.PantryOf(TestData.Stocked("tomatoes", 6), TestData.Stocked("basil", 2, "bunch"));
        var request = new PlanRequest(TestData.Sunday, TestData.Library, new HashSet<Guid>(), pantry, DateTimeOffset.UnixEpoch, PantryBoost: 4);

        var titles = TestData.Dinners(TestData.Planning.Plan(request).Plan).Select(dinner => dinner.Title).ToList();

        // ...but with them bought, both tomato-and-basil meals are planned.
        await Assert.That(titles.Contains(TestData.Caprese.Name)).IsTrue();
        await Assert.That(titles.Contains(TestData.TomatoSalad.Name)).IsTrue();
    }

    [Test]
    public async Task Counting_a_backup_reads_what_it_holds_and_treats_missing_lists_as_empty()
    {
        var folder = Directory.CreateTempSubdirectory("chefbot-backup-test").FullName;
        try
        {
            // An older backup: it has no groceryStores list.
            const string name = "chefbot-backup-test.json.gz";
            const string json = """
                {
                  "createdAt": "2026-01-01T00:00:00Z",
                  "recipes": [{ "id": 1, "name": "Soup", "ingredients": [{ "id": 1 }] }, { "id": 2 }],
                  "recipeImageData": [{ "id": 1, "data": "AAEC" }],
                  "mealPlans": [{ "id": 1, "meals": [{ "id": 1 }, { "id": 2 }, { "id": 3 }], "shoppingItems": [{ "id": 1 }, { "id": 2 }] }],
                  "pantryItems": [],
                  "users": [{ "id": 1, "email": "admin@example.com" }]
                }
                """;
            await using (var file = File.Create(Path.Combine(folder, name)))
            await using (var zip = new GZipStream(file, CompressionLevel.Fastest))
            {
                await zip.WriteAsync(Encoding.UTF8.GetBytes(json));
            }

            var counts = await new BackupAccess(null!, folder).CountAsync(name, CancellationToken.None);

            await Assert.That(counts).IsEqualTo(new BackupCounts(2, 1, 1, 3, 2, 0, 0, 1, 0));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
