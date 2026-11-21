using System.Net;
using System.Text.Json;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChefBot.Core.Tests;

/// <summary>What each kind of to-do app is sent, checked against a recording stand-in for the app.</summary>
public class ToDoListAccessTests
{
    private static readonly ToDoItem[] Items = [new("fish fillets, 600 g", "Corner Market · For Slaw Fish Tacos"), new("limes, 3")];

    private static (ToDoListAccess Access, RecordingHandler Server) Connect()
    {
        var server = new RecordingHandler();
        return (new ToDoListAccess(new HttpClient(server), new FixedClock(), NullLogger<ToDoListAccess>.Instance), server);
    }

    private static ShoppingTarget Target(ShoppingTargetKind kind, string url = "", string list = "", string token = "secret") =>
        new() { Name = "Kitchen list", Kind = kind, Url = url, List = list, Token = token };

    [Test]
    public async Task Home_assistant_gets_each_item_through_todo_add_item()
    {
        var (access, server) = Connect();

        var delivery = await access.SendAsync(
            Target(ShoppingTargetKind.HomeAssistant, "http://homeassistant.local:8123/", "todo.shopping_list"), Items, default);

        await Assert.That(delivery).IsEqualTo(new ToDoDelivery(2));
        var (request, body) = server.Requests[0];
        await Assert.That(request.RequestUri!.AbsoluteUri).IsEqualTo("http://homeassistant.local:8123/api/services/todo/add_item");
        await Assert.That(request.Headers.Authorization!.ToString()).IsEqualTo("Bearer secret");
        await Assert.That(body).IsEqualTo("""{"entity_id":"todo.shopping_list","item":"fish fillets, 600 g"}""");
    }

    [Test]
    public async Task Todoist_gets_a_task_per_item_in_the_inbox_unless_a_project_is_named()
    {
        var (access, server) = Connect();

        await access.SendAsync(Target(ShoppingTargetKind.Todoist), Items, default);

        await Assert.That(server.Requests.Select(sent => sent.Request.RequestUri)).All().Satisfy(uri => uri.IsEqualTo(ToDoListAccess.TodoistTasks));
        using var first = JsonDocument.Parse(server.Requests[0].Body);
        await Assert.That(first.RootElement.GetProperty("content").GetString()).IsEqualTo("fish fillets, 600 g");
        await Assert.That(first.RootElement.GetProperty("description").GetString()).IsEqualTo("Corner Market · For Slaw Fish Tacos");
        await Assert.That(first.RootElement.TryGetProperty("project_id", out _)).IsFalse();
        await Assert.That(server.Requests[1].Body).IsEqualTo("""{"content":"limes, 3"}""");
    }

    [Test]
    public async Task A_webhook_gets_the_whole_list_at_once()
    {
        var (access, server) = Connect();

        var delivery = await access.SendAsync(Target(ShoppingTargetKind.Webhook, "https://hooks.example/list", token: ""), Items, default);

        await Assert.That(delivery.Sent).IsEqualTo(2);
        var (request, body) = server.Requests.Single();
        await Assert.That(request.Headers.Authorization).IsNull();
        using var json = JsonDocument.Parse(body);
        await Assert.That(json.RootElement.GetProperty("type").GetString()).IsEqualTo("shopping_list");
        await Assert.That(json.RootElement.GetProperty("items").GetArrayLength()).IsEqualTo(2);
        await Assert.That(json.RootElement.GetProperty("items")[1].GetProperty("title").GetString()).IsEqualTo("limes, 3");
    }

    [Test]
    public async Task A_refused_token_stops_at_the_first_item_and_says_what_to_check()
    {
        var (access, server) = Connect();
        server.Status = HttpStatusCode.Unauthorized;

        var delivery = await access.SendAsync(Target(ShoppingTargetKind.HomeAssistant, "http://ha:8123", "todo.shopping_list"), Items, default);

        await Assert.That(delivery.Sent).IsEqualTo(0);
        await Assert.That(delivery.Problem).IsEqualTo("Kitchen list didn't accept the access token (HTTP 401). Check the token in Settings.");
        await Assert.That(server.Requests.Count).IsEqualTo(1);
    }
}
