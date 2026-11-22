namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class NotificationSettingsEntity
{
    public int Id { get; set; } = 1;

    public string WebhookUrl { get; set; } = string.Empty;

    public bool PlanReady { get; set; }

    public bool SlowCooker { get; set; }

    public bool MealsToday { get; set; }

    public bool ExpiringFood { get; set; }

    public bool Shopping { get; set; }

    public bool Achievements { get; set; } = true;

    public DateTimeOffset? RemindersDueAt { get; set; }

    public string DailyTime { get; set; } = string.Empty;

    public DateOnly? LastDailyRunOn { get; set; }
}
