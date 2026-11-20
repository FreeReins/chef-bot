using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;
using Microsoft.Extensions.Logging;

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
