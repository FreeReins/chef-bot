namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class AchievementUnlockEntity
{
    public Guid Id { get; set; }

    /// <summary>The achievement's kind by name, or "Rank" for a chef rank.</summary>
    public string Key { get; set; } = string.Empty;

    public int Tier { get; set; }

    public DateTimeOffset EarnedAt { get; set; }

    public bool Quiet { get; set; }

    public Guid? CausedBy { get; set; }
}
