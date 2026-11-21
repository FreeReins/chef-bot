using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Sends shopping items to to-do apps, one way per <see cref="ShoppingTargetKind"/>:
/// <list type="bullet">
/// <item>Home Assistant: <c>POST {url}/api/services/todo/add_item</c> with <c>{ "entity_id", "item" }</c> for each
/// item. The description is left out, since Home Assistant's own Shopping List refuses one.</item>
/// <item>Todoist: <c>POST https://api.todoist.com/api/v1/tasks</c> with <c>{ "content", "description", "project_id" }</c>
/// for each item.</item>
/// <item>A webhook: one <c>POST</c> of <c>{ "type": "shopping_list", "sentAt", "items": [{ "title", "description" }] }</c>.</item>
/// </list>
/// The token goes in an <c>Authorization: Bearer</c> header.
/// </summary>
internal sealed partial class ToDoListAccess(HttpClient http, TimeProvider clock, ILogger<ToDoListAccess> logger) : IToDoListAccess
{
    /// <summary>Where Todoist's API takes new tasks.</summary>
    public static readonly Uri TodoistTasks = new("https://api.todoist.com/api/v1/tasks");

    public async Task<ToDoDelivery> SendAsync(ShoppingTarget target, IReadOnlyList<ToDoItem> items, CancellationToken cancellationToken)
    {
        try
        {
            return target.Kind switch
            {
                ShoppingTargetKind.HomeAssistant => await EachAsync(target, items, HomeAssistantAddress(target.Url), item =>
                    JsonContent.Create(new HomeAssistantItem(target.List, item.Title), ToDoJson.Default.HomeAssistantItem), cancellationToken),
                ShoppingTargetKind.Todoist => await EachAsync(target, items, TodoistTasks, item =>
                    JsonContent.Create(new TodoistTask(item.Title, item.Description, Blank(target.List)), ToDoJson.Default.TodoistTask), cancellationToken),
                _ => await WebhookAsync(target, items, cancellationToken),
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            logger.LogWarning("{Target} couldn't be reached: {Reason}", target.Name, exception.Message);
            return new ToDoDelivery(0, $"{target.Name} couldn't be reached: {exception.Message}");
        }
    }

    /// <summary>Home Assistant's service address under the address it is reached at, with or without a trailing slash.</summary>
    public static Uri HomeAssistantAddress(string url) => new($"{url.TrimEnd('/')}/api/services/todo/add_item");

    /// <summary>Posts one item at a time, stopping at the first the app refuses.</summary>
    private async Task<ToDoDelivery> EachAsync(
        ShoppingTarget target, IReadOnlyList<ToDoItem> items, Uri address, Func<ToDoItem, HttpContent> body, CancellationToken cancellationToken)
    {
        for (var sent = 0; sent < items.Count; sent++)
        {
            try
            {
                if (await PostAsync(target, address, body(items[sent]), cancellationToken) is { } problem)
                {
                    return new ToDoDelivery(sent, problem);
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning("{Target} couldn't be reached: {Reason}", target.Name, exception.Message);
                return new ToDoDelivery(sent, $"{target.Name} couldn't be reached: {exception.Message}");
            }
        }

        return new ToDoDelivery(items.Count);
    }

    private async Task<ToDoDelivery> WebhookAsync(ShoppingTarget target, IReadOnlyList<ToDoItem> items, CancellationToken cancellationToken)
    {
        var payload = new WebhookList("shopping_list", clock.GetUtcNow(), items.Select(item => new WebhookItem(item.Title, item.Description)).ToList());
        var problem = await PostAsync(target, new Uri(target.Url), JsonContent.Create(payload, ToDoJson.Default.WebhookList), cancellationToken);
        return problem is null ? new ToDoDelivery(items.Count) : new ToDoDelivery(0, problem);
    }

    /// <summary>Null when the app took it; otherwise what to tell the person.</summary>
    private async Task<string?> PostAsync(ShoppingTarget target, Uri address, HttpContent body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, address) { Content = body };
        if (target.Token.Length > 0)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", target.Token);
        }

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return null;

        var status = (int)response.StatusCode;
        logger.LogWarning("{Target} ({Kind}) answered HTTP {Status}.", target.Name, target.Kind, status);
        return status is 401 or 403
            ? $"{target.Name} didn't accept the access token (HTTP {status}). Check the token in Settings."
            : status == 404 && target.Kind == ShoppingTargetKind.HomeAssistant
                ? $"{target.Name} has no to-do list called {target.List}, or its address is wrong (HTTP 404). Check both in Settings."
                : $"{target.Name} answered HTTP {status}. Check its settings, then try again.";
    }

    private static string? Blank(string text) => text.Length == 0 ? null : text;

    /// <summary>The body of Home Assistant's <c>todo.add_item</c>.</summary>
    internal sealed record HomeAssistantItem(
        [property: JsonPropertyName("entity_id")] string EntityId,
        [property: JsonPropertyName("item")] string Item);

    /// <summary>A new Todoist task. A missing project puts it in the Inbox.</summary>
    internal sealed record TodoistTask(
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("project_id")] string? ProjectId);

    /// <summary>What a webhook receives: the whole list at once.</summary>
    internal sealed record WebhookList(string Type, DateTimeOffset SentAt, IReadOnlyList<WebhookItem> Items);

    /// <summary>One item in <see cref="WebhookList"/>.</summary>
    internal sealed record WebhookItem(string Title, string? Description);

    /// <summary>The bodies sent to to-do apps, made at build time. Empty values are left out.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(HomeAssistantItem))]
    [JsonSerializable(typeof(TodoistTask))]
    [JsonSerializable(typeof(WebhookList))]
    internal sealed partial class ToDoJson : JsonSerializerContext;
}
