namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The notification settings, and the last day the daily reminders went out.</summary>
public sealed record NotificationSettingsResponse(
    string WebhookUrl,
    bool PlanReady,
    bool SlowCooker,
    bool MealsToday,
    bool ExpiringFood,
    bool Shopping,
    string DailyTime,
    DateOnly? LastDailyRunOn,
    bool Achievements);
