using System.Net.Http.Json;
using ChefBot.Core.Notifications;
using Microsoft.Extensions.Logging;

namespace ChefBot.Infrastructure.Notifications;

/// <summary>
/// Posts notifications to a webhook as JSON:
/// <c>{ "type": "slow_cooker", "title": "...", "message": "...", "sentAt": "...", "data": { ... } }</c>.
/// </summary>
public sealed class WebhookNotificationSender(HttpClient http, TimeProvider clock, ILogger<WebhookNotificationSender> logger)
    : INotificationSender
{
    public async Task<DeliveryResult> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhook))
        {
            return new DeliveryResult(false, "The webhook address isn't valid.");
        }

        var payload = new
        {
            type = notification.Type,
            title = notification.Title,
            message = notification.Message,
            sentAt = clock.GetUtcNow(),
            data = notification.Data,
        };

        try
        {
            using var response = await http.PostAsJsonAsync(webhook, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new DeliveryResult(true);
            }

            logger.LogWarning("The notification webhook answered HTTP {Status} for {Type}.", (int)response.StatusCode, notification.Type);
            return new DeliveryResult(false, $"The webhook answered HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("The notification webhook couldn't be reached for {Type}: {Reason}", notification.Type, exception.Message);
            return new DeliveryResult(false, $"The webhook couldn't be reached: {exception.Message}");
        }
    }
}
