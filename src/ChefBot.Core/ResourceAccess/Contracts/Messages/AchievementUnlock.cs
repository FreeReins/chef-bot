using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// A tier of an achievement, or a chef rank, the household reached. Kept once earned, even if what earned it goes
/// away later (a review deleted, a recipe archived), so it is announced only once.
/// </summary>
internal sealed class AchievementUnlock
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The achievement, or null for a chef rank.</summary>
    public AchievementKind? Kind { get; set; }

    /// <summary>The tier reached (1 for a first step), or the rank number for a chef rank.</summary>
    public int Tier { get; set; }

    public DateTimeOffset EarnedAt { get; set; }

    /// <summary>
    /// Recorded without telling anyone: everything already reached when ChefBot first kept track, so nobody is
    /// flooded with old news.
    /// </summary>
    public bool Quiet { get; set; }

    /// <summary>The message that led to the check that found it, for the log.</summary>
    public Guid? CausedBy { get; set; }
}
