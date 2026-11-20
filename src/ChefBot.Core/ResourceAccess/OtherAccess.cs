using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.Extensions.Logging;
using Wolverine.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Posts notifications to a webhook as JSON:
/// <c>{ "type": "slow_cooker", "title": "...", "message": "...", "sentAt": "...", "data": { ... } }</c>.
/// </summary>
internal sealed partial class NotificationAccess(HttpClient http, TimeProvider clock, ILogger<NotificationAccess> logger) : INotificationAccess
{
    public async Task<Delivery> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhook))
        {
            return new Delivery(false, "The webhook address isn't valid.");
        }

        var payload = new WebhookPayload(notification.Type, notification.Title, notification.Message, clock.GetUtcNow(), notification.Data);

        try
        {
            using var response = await http.PostAsJsonAsync(webhook, payload, WebhookJson.Default.WebhookPayload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new Delivery(true);
            }

            logger.LogWarning("The notification webhook answered HTTP {Status} for {Type}.", (int)response.StatusCode, notification.Type);
            return new Delivery(false, $"The webhook answered HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("The notification webhook couldn't be reached for {Type}: {Reason}", notification.Type, exception.Message);
            return new Delivery(false, $"The webhook couldn't be reached: {exception.Message}");
        }
    }

    /// <summary>What the webhook receives.</summary>
    internal sealed record WebhookPayload(string Type, string Title, string Message, DateTimeOffset SentAt, IReadOnlyDictionary<string, object?> Data);

    /// <summary>
    /// The webhook's JSON shape, made at build time. <see cref="Notification.Data"/> holds plain values, so each kind
    /// of value <see cref="Engines.NotificationEngine"/> puts in it is listed here; a new kind needs a line too.
    /// </summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(WebhookPayload))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(DateOnly))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, object?>))]
    [JsonSerializable(typeof(List<Dictionary<string, object?>>))]
    internal sealed partial class WebhookJson : JsonSerializerContext;
}

/// <summary>The starter recipes offered by the setup wizard, read once from config/sample-recipes.json.</summary>
internal sealed partial class SampleRecipeAccess(string path) : ISampleRecipeAccess
{
    private readonly Lazy<IReadOnlyList<RecipeText>> _all = new(() => Load(path));

    public IReadOnlyList<RecipeText> All() => _all.Value;

    private static IReadOnlyList<RecipeText> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        return JsonSerializer.Deserialize(File.ReadAllText(path), SampleJson.Default.ListRecipeText) ?? [];
    }

    /// <summary>The sample recipe file's JSON shape, made at build time.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
    [JsonSerializable(typeof(List<RecipeText>))]
    private sealed partial class SampleJson : JsonSerializerContext;
}

/// <summary>
/// Saves the database changes and sends the queued messages together, through Wolverine's EF Core outbox: the
/// messages are written to the database in the same transaction and sent once it commits.
/// </summary>
internal sealed class OutboxUnitOfWork(IDbContextOutbox<ChefBotDbContext> outbox) : IUnitOfWork
{
    public Task PublishAsync(object message) => outbox.PublishAsync(message).AsTask();

    public Task CommitAsync(CancellationToken cancellationToken) => outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
}
