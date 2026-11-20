using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// Notifications sent to a webhook, for example Home Assistant. Admins only.
/// Each notification is a JSON POST with <c>type</c>, <c>title</c>, <c>message</c>, <c>sentAt</c> and <c>data</c>.
/// </summary>
[Authorize(Policy = Sessions.AdminPolicy)]
public static class NotificationEndpoints
{
    [WolverineGet("/api/system/notifications")]
    public static Task<NotificationSettingsResponse> GetSettings([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.GetNotificationSettingsAsync(cancellationToken);

    /// <summary>Saves the webhook address, which reminders are wanted and when the daily ones go out.</summary>
    [WolverinePut("/api/system/notifications")]
    public static Task<NotificationSettingsResponse> UpdateSettings(
        NotificationSettingsRequest request, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.UpdateNotificationSettingsAsync(request, cancellationToken);

    /// <summary>Sends a sample notification to check the webhook is reachable.</summary>
    [WolverinePost("/api/system/notifications/test")]
    public static Task<DeliveryResult> SendTest([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.SendTestNotificationAsync(cancellationToken);

    /// <summary>Sends today's reminders now instead of waiting for the daily time.</summary>
    [WolverinePost("/api/system/notifications/send-today")]
    public static Task<ReminderRun> SendToday([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.SendTodayAsync(cancellationToken);
}
