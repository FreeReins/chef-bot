using ChefBot.Core.Abstractions;
using ChefBot.Core.Common;
using ChefBot.Core.Planning;

namespace ChefBot.Core.Notifications;

/// <summary>The outcome of sending a batch of reminders.</summary>
/// <param name="Sent">The types that were delivered.</param>
/// <param name="Problems">What went wrong for those that weren't.</param>
public sealed record ReminderRun(IReadOnlyList<string> Sent, IReadOnlyList<string> Problems);

/// <summary>Sends plan and daily reminders to the configured webhook, and manages their settings.</summary>
public sealed class ReminderService(
    ISettingsStore settings,
    IMealPlanRepository plans,
    IPantryRepository pantry,
    INotificationSender sender,
    IUnitOfWork unitOfWork,
    PlannerOptions options,
    TimeProvider clock)
{
    public Task<NotificationSettings> GetSettingsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync<NotificationSettings>(cancellationToken);

    /// <summary>Saves the settings chosen on the Settings tab.</summary>
    public async Task<NotificationSettings> UpdateSettingsAsync(NotificationSettings changes, CancellationToken cancellationToken)
    {
        var url = changes.WebhookUrl?.Trim() ?? string.Empty;
        if (url.Length > 0 && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"))
        {
            throw new RuleViolationException("The webhook must be a full http or https address.");
        }

        var current = await GetSettingsAsync(cancellationToken);
        current.WebhookUrl = url;
        current.PlanReady = changes.PlanReady;
        current.SlowCooker = changes.SlowCooker;
        current.MealsToday = changes.MealsToday;
        current.ExpiringFood = changes.ExpiringFood;
        current.Shopping = changes.Shopping;
        current.DailyTime = DailyTime.Format(DailyTime.Parse(changes.DailyTime));
        await unitOfWork.CommitAsync(cancellationToken);
        return current;
    }

    /// <summary>Uses a webhook from the configuration file as the starting value, when none has been saved.</summary>
    public async Task UseDefaultWebhookAsync(string? webhookUrl, CancellationToken cancellationToken)
    {
        var current = await GetSettingsAsync(cancellationToken);
        if (current.WebhookUrl.Length == 0 && !string.IsNullOrWhiteSpace(webhookUrl))
        {
            current.WebhookUrl = webhookUrl.Trim();
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>Sends a sample notification so the receiving side can be checked.</summary>
    public async Task<DeliveryResult> SendTestAsync(CancellationToken cancellationToken)
    {
        var current = await GetSettingsAsync(cancellationToken);
        if (current.WebhookUrl.Length == 0)
        {
            return new DeliveryResult(false, "Enter a webhook address and save it first.");
        }

        var test = new Notification(NotificationTypes.Test, "ChefBot test", "Notifications from ChefBot are working.",
            new Dictionary<string, object?> { ["sentBy"] = "Settings" });
        return await sender.SendAsync(current.WebhookUrl, test, cancellationToken);
    }

    /// <summary>Announces a newly staged plan.</summary>
    public async Task<ReminderRun> SendPlanReadyAsync(Guid planId, string trigger, CancellationToken cancellationToken)
    {
        var plan = await plans.FindAsync(planId, cancellationToken);
        return plan is null
            ? new ReminderRun([], [])
            : await SendAsync([ReminderBuilder.PlanReady(plan, trigger)], cancellationToken);
    }

    /// <summary>Sends today's reminders if their time has come and they haven't gone out yet today.</summary>
    public async Task<ReminderRun?> SendDailyIfDueAsync(CancellationToken cancellationToken)
    {
        var current = await GetSettingsAsync(cancellationToken);
        if (!DailyTime.IsDue(current.DailyTime, current.LastDailyRunOn, clock.GetLocalNow()))
        {
            return null;
        }

        return await SendDailyAsync(cancellationToken);
    }

    /// <summary>Sends today's reminders now, whatever the time.</summary>
    public async Task<ReminderRun> SendDailyAsync(CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var current = await GetSettingsAsync(cancellationToken);
        var plan = await plans.FindCurrentAsync(today, cancellationToken);
        var reminders = ReminderBuilder.Daily(plan, await pantry.ListAsync(cancellationToken), today, options.ExpiringSoonDays);

        var run = await SendAsync(reminders, cancellationToken);
        current.LastDailyRunOn = today;
        await unitOfWork.CommitAsync(cancellationToken);
        return run;
    }

    private async Task<ReminderRun> SendAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken)
    {
        var current = await GetSettingsAsync(cancellationToken);
        var sent = new List<string>();
        var problems = new List<string>();
        if (current.WebhookUrl.Length == 0)
        {
            return new ReminderRun(sent, problems);
        }

        foreach (var notification in notifications.Where(notification => current.IsWanted(notification.Type)))
        {
            var result = await sender.SendAsync(current.WebhookUrl, notification, cancellationToken);
            if (result.Delivered)
            {
                sent.Add(notification.Type);
            }
            else
            {
                problems.Add($"{notification.Type}: {result.Problem}");
            }
        }

        return new ReminderRun(sent, problems);
    }
}
