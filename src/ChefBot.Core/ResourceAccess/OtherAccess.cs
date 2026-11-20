using System.Net.Http.Json;
using System.Text.Json;
using ChefBot.Core.ResourceAccess.Contracts;
using ChefBot.Core.ResourceAccess.Persistence;
using Microsoft.Extensions.Logging;
using Wolverine.EntityFrameworkCore;

namespace ChefBot.Core.ResourceAccess;

/// <summary>
/// Posts notifications to a webhook as JSON:
/// <c>{ "type": "slow_cooker", "title": "...", "message": "...", "sentAt": "...", "data": { ... } }</c>.
/// </summary>
internal sealed class NotificationAccess(HttpClient http, TimeProvider clock, ILogger<NotificationAccess> logger) : INotificationAccess
{
    public async Task<Delivery> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var webhook))
        {
            return new Delivery(false, "The webhook address isn't valid.");
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
}

/// <summary>The starter recipes offered by the setup wizard, read once from config/sample-recipes.json.</summary>
internal sealed class SampleRecipeAccess(string path) : ISampleRecipeAccess
{
    private readonly Lazy<IReadOnlyList<RecipeText>> _all = new(() => Load(path));

    public IReadOnlyList<RecipeText> All() => _all.Value;

    private static IReadOnlyList<RecipeText> Load(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Deserialize<List<RecipeText>>(File.ReadAllText(path), options) ?? [];
    }
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
