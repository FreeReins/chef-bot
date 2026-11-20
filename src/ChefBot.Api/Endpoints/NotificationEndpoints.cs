using System.Security.Claims;
using ChefBot.Core.Accounts;
using ChefBot.Core.Notifications;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>
/// Notifications sent to a webhook, for example Home Assistant. Admins only.
/// Each notification is a JSON POST with <c>type</c>, <c>title</c>, <c>message</c>, <c>sentAt</c> and <c>data</c>.
/// </summary>
public static class NotificationEndpoints
{
    [WolverineGet("/api/system/notifications")]
    public static async Task<NotificationSettings> GetSettings(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] ReminderService reminders,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await reminders.GetSettingsAsync(cancellationToken);
    }

    /// <summary>Saves the webhook address, which reminders are wanted and when the daily ones go out.</summary>
    [WolverinePut("/api/system/notifications")]
    public static async Task<NotificationSettings> UpdateSettings(
        NotificationSettings request,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] ReminderService reminders,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await reminders.UpdateSettingsAsync(request, cancellationToken);
    }

    /// <summary>Sends a sample notification to check the webhook is reachable.</summary>
    [WolverinePost("/api/system/notifications/test")]
    public static async Task<DeliveryResult> SendTest(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] ReminderService reminders,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await reminders.SendTestAsync(cancellationToken);
    }

    /// <summary>Sends today's reminders now instead of waiting for the daily time.</summary>
    [WolverinePost("/api/system/notifications/send-today")]
    public static async Task<ReminderRun> SendToday(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] ReminderService reminders,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await reminders.SendDailyAsync(cancellationToken);
    }
}
