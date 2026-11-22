using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChefBot.Core.Tests;

/// <summary>
/// ChefBot's JSON shapes are made at build time by the source generator. These tests check that nothing is missing
/// from them and that they read and write JSON exactly as the reflection-based serializer did before.
/// </summary>
public class JsonShapeTests
{
    private static readonly Assembly Core = typeof(CoreRegistration).Assembly;

    private static IEnumerable<Type> ContractTypes() =>
        Core.GetExportedTypes().Where(type => type.Namespace == "ChefBot.Core.Managers.Contracts.Messages" && !(type.IsAbstract && type.IsSealed));

    [Test]
    public async Task Every_request_response_command_and_event_has_a_build_time_shape()
    {
        var missing = ContractTypes().Where(type => ContractJsonContext.Default.GetTypeInfo(type) is null).Select(type => type.Name).ToList();

        await Assert.That(missing).IsEmpty();
    }

    [Test]
    public async Task Enums_are_written_as_text()
    {
        var numeric = ContractTypes()
            .Where(type => type.IsEnum)
            .Where(type => !JsonSerializer.Serialize(Enum.GetValues(type).GetValue(0), ContractJsonContext.Default.GetTypeInfo(type)!).StartsWith('"'))
            .Select(type => type.Name)
            .ToList();

        await Assert.That(numeric).IsEmpty();
    }

    [Test]
    public async Task Days_of_the_week_are_written_by_name()
    {
        var rule = new Managers.Contracts.Messages.PlanRuleDetails(
            Managers.Contracts.Messages.RuleLimit.AtLeast, 1, Managers.Contracts.Messages.MealSlot.Dinner,
            Managers.Contracts.Messages.RecipeMatchKind.SlowCooker, Day: DayOfWeek.Tuesday);

        var json = JsonSerializer.Serialize(rule, ContractJsonContext.Default.PlanRuleDetails);

        await Assert.That(json).Contains("\"day\":\"Tuesday\"");
        await Assert.That(JsonSerializer.Deserialize(json, ContractJsonContext.Default.PlanRuleDetails)).IsEqualTo(rule with { Values = null });
    }

    /// <summary>
    /// Reads the smallest valid JSON for every type each build-time shape can reach, with that shape and with
    /// reflection, and compares the results. A field the JSON leaves out must keep its default the same way.
    /// </summary>
    [Test]
    public async Task Build_time_shapes_read_json_the_way_reflection_does()
    {
        var differences = new List<string>();
        foreach (var context in Contexts())
        {
            var reflection = new JsonSerializerOptions(context.Options) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
            foreach (var shape in Reachable(context).Where(shape => shape.Kind == JsonTypeInfoKind.Object))
            {
                var expected = reflection.GetTypeInfo(shape.Type);
                var names = string.Join(",", shape.Properties.Select(property => property.Name));
                var expectedNames = string.Join(",", expected.Properties.Select(property => property.Name));
                if (names != expectedNames)
                {
                    differences.Add($"{shape.Type.Name} has fields {names}, expected {expectedNames}");
                    continue;
                }

                var json = SmallestJson(expected, reflection);
                var read = Outcome(() => JsonSerializer.Serialize(JsonSerializer.Deserialize(json, shape), shape.Type, reflection));
                var expectedRead = Outcome(() => JsonSerializer.Serialize(JsonSerializer.Deserialize(json, shape.Type, reflection), shape.Type, reflection));
                if (read != expectedRead)
                {
                    differences.Add($"{shape.Type.Name} read {json} as {read}, expected {expectedRead}");
                }
            }
        }

        await Assert.That(differences).IsEmpty();
    }

    [Test]
    public async Task Every_kind_of_notification_reaches_the_webhook_as_before()
    {
        var plan = TestData.PlanWeek(TestData.Sunday).Plan;
        TestData.Shopping.ApplyList(plan, TestData.Shopping.BuildList(plan, PantrySnapshot.Empty, []));
        var tomatoes = TestData.Stocked("tomatoes", 4, expiresOn: TestData.Sunday.AddDays(4));
        var notifications = new List<Notification> { TestData.Notifications.PlanReady(plan, "schedule"), TestData.Notifications.Test() };
        notifications.AddRange(TestData.Notifications.Daily(plan, [tomatoes], TestData.Sunday.AddDays(-1), 3));
        notifications.AddRange(TestData.Notifications.Daily(plan, [tomatoes], TestData.Sunday.AddDays(2), 3));
        notifications.Add(TestData.Notifications.Achievements(
            [new AchievementUnlockResponse(Guid.NewGuid(), AchievementKind.Dinners, 2, DateTimeOffset.UnixEpoch), new AchievementUnlockResponse(Guid.NewGuid(), null, 1, DateTimeOffset.UnixEpoch)]));

        var webhook = new CapturingHandler();
        var clock = new FixedClock();
        var access = new NotificationAccess(new HttpClient(webhook), clock, NullLogger<NotificationAccess>.Instance);
        foreach (var notification in notifications)
        {
            var delivery = await access.SendAsync("https://hooks.example/chefbot", notification, CancellationToken.None);
            await Assert.That(delivery.Delivered).IsTrue();
        }

        // Before build-time shapes, the payload was this anonymous object written by reflection.
        var reflection = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var expected = notifications.Select(notification => JsonSerializer.Serialize(
            new { type = notification.Type, title = notification.Title, message = notification.Message, sentAt = clock.GetUtcNow(), data = notification.Data },
            reflection));
        var kinds = notifications.Select(notification => notification.Type).ToHashSet();

        await Assert.That(webhook.Bodies).IsEquivalentTo(expected);
        await Assert.That(kinds).IsEquivalentTo(typeof(NotificationTypes).GetFields().Select(field => (string)field.GetValue(null)!));
    }

    /// <summary>Every source-generated context in the core, including the private ones inside accessors.</summary>
    private static IEnumerable<JsonSerializerContext> Contexts() =>
        Core.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(JsonSerializerContext)))
            .Select(type => (JsonSerializerContext)type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!);

    /// <summary>The types listed on a context and every type their fields, items and keys use.</summary>
    private static IEnumerable<JsonTypeInfo> Reachable(JsonSerializerContext context)
    {
        var seen = new HashSet<Type>();
        var pending = new Queue<Type>(context.GetType().GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType == typeof(JsonSerializableAttribute))
            .Select(attribute => (Type)attribute.ConstructorArguments[0].Value!));

        while (pending.TryDequeue(out var type))
        {
            if (!seen.Add(type) || context.GetTypeInfo(type) is not { } shape)
            {
                continue;
            }

            yield return shape;
            foreach (var used in shape.Properties.Select(property => property.PropertyType).Append(shape.ElementType).Append(shape.KeyType))
            {
                if (used is not null)
                {
                    pending.Enqueue(used);
                }
            }
        }
    }

    /// <summary>An object with only its required fields, each set to the empty value of its type.</summary>
    private static string SmallestJson(JsonTypeInfo shape, JsonSerializerOptions options)
    {
        var fields = shape.Properties
            .Where(property => property.IsRequired)
            .Select(property => $"{JsonSerializer.Serialize(property.Name)}:{JsonSerializer.Serialize(Empty(property.PropertyType), property.PropertyType, options)}");
        return "{" + string.Join(",", fields) + "}";
    }

    /// <summary>The JSON written, or the kind of exception thrown: some records compute fields that need real data.</summary>
    private static string Outcome(Func<string> write)
    {
        try
        {
            return write();
        }
        catch (Exception exception)
        {
            return $"throws {exception.GetType().Name}";
        }
    }

    private static object? Empty(Type type) =>
        type == typeof(string) ? "" : type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary>Stands in for the webhook: answers 200 and keeps each body it was sent.</summary>
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(Encoding.UTF8.GetString(await request.Content!.ReadAsByteArrayAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
