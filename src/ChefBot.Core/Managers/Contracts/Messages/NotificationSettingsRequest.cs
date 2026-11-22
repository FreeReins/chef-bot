namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Where notifications go, which ones are wanted, and when the daily ones go out.</summary>
/// <param name="WebhookUrl">A URL that accepts a JSON POST, such as a Home Assistant webhook. Empty turns notifications off.</param>
/// <param name="PlanReady">Announce each newly staged plan.</param>
/// <param name="SlowCooker">Remind in the morning when tonight's dinner is a slow cooker meal.</param>
/// <param name="MealsToday">List what is planned for today.</param>
/// <param name="ExpiringFood">Warn about pantry food that is about to expire.</param>
/// <param name="Shopping">Remind to confirm the plan or finish the shopping as the week starts.</param>
/// <param name="DailyTime">Local time the daily reminders are sent, as HH:mm.</param>
/// <param name="Achievements">Announce new achievement tiers and chef ranks. On when left out.</param>
public sealed record NotificationSettingsRequest(
    string? WebhookUrl,
    bool PlanReady,
    bool SlowCooker,
    bool MealsToday,
    bool ExpiringFood,
    bool Shopping,
    string DailyTime,
    bool Achievements = true);
