namespace ChefBot.Core.Notifications;

/// <summary>The kinds of notification ChefBot sends. The value is the <c>type</c> field of the webhook payload.</summary>
public static class NotificationTypes
{
    /// <summary>A new plan was staged and is waiting for review.</summary>
    public const string PlanReady = "plan_ready";

    /// <summary>Tonight's dinner is a slow cooker meal that needs starting.</summary>
    public const string SlowCooker = "slow_cooker";

    /// <summary>What is planned for today.</summary>
    public const string MealsToday = "meals_today";

    /// <summary>Food in the pantry is about to expire.</summary>
    public const string ExpiringFood = "expiring_food";

    /// <summary>The week is about to start and the plan is unconfirmed or has groceries left to buy.</summary>
    public const string Shopping = "shopping";

    /// <summary>Sent from the Settings tab to check the webhook works.</summary>
    public const string Test = "test";
}

/// <summary>Something worth telling the household.</summary>
/// <param name="Type">One of <see cref="NotificationTypes"/>.</param>
/// <param name="Title">A short heading.</param>
/// <param name="Message">A ready-to-show sentence or two.</param>
/// <param name="Data">The facts behind the message, so the receiver can word its own.</param>
public sealed record Notification(string Type, string Title, string Message, IReadOnlyDictionary<string, object?> Data);

/// <summary>Whether a notification reached the webhook.</summary>
public sealed record DeliveryResult(bool Delivered, string? Problem = null);

/// <summary>Delivers notifications to the outside world.</summary>
public interface INotificationSender
{
    /// <summary>Posts a notification as JSON. Failures are reported in the result, never thrown.</summary>
    Task<DeliveryResult> SendAsync(string webhookUrl, Notification notification, CancellationToken cancellationToken);
}

/// <summary>Where notifications go and which ones are wanted. One stored row, changed on the Settings tab.</summary>
public sealed class NotificationSettings
{
    public int Id { get; set; } = 1;

    /// <summary>
    /// A URL that accepts a JSON POST, such as a Home Assistant webhook. Empty turns notifications off.
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;

    public bool PlanReady { get; set; } = true;

    public bool SlowCooker { get; set; } = true;

    public bool MealsToday { get; set; }

    public bool ExpiringFood { get; set; } = true;

    public bool Shopping { get; set; } = true;

    /// <summary>Local time the daily reminders are sent, as HH:mm.</summary>
    public string DailyTime { get; set; } = "07:30";

    /// <summary>The last day the daily reminders ran, so they go out once a day.</summary>
    public DateOnly? LastDailyRunOn { get; set; }

    public bool IsWanted(string type) => type switch
    {
        NotificationTypes.PlanReady => PlanReady,
        NotificationTypes.SlowCooker => SlowCooker,
        NotificationTypes.MealsToday => MealsToday,
        NotificationTypes.ExpiringFood => ExpiringFood,
        NotificationTypes.Shopping => Shopping,
        _ => true,
    };
}
