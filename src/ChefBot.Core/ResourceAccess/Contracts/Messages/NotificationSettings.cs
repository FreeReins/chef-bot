namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>Where notifications go and which ones are wanted.</summary>
internal sealed class NotificationSettings
{
    /// <summary>A URL that accepts a JSON POST, such as a Home Assistant webhook. Empty turns notifications off.</summary>
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
}
