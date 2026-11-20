using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.ResourceAccess;

/// <summary>Delivers notifications to the webhook.</summary>
internal interface INotificationAccess
{
    /// <summary>Posts a notification as JSON. Failures are reported in the result, never thrown.</summary>
    Task<Delivery> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken);
}
