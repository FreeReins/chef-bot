using ChefBot.Core.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines;

/// <summary>How fresh a pantry item is on a given day.</summary>
internal static class PantryFreshness
{
    public static bool IsExpired(this PantryItem item, DateOnly today) => item.ExpiresOn is DateOnly date && date < today;

    /// <summary>Days until it expires. Negative once it has, null when no date is known.</summary>
    public static int? DaysLeft(this PantryItem item, DateOnly today) =>
        item.ExpiresOn is DateOnly date ? date.DayNumber - today.DayNumber : null;

    public static Freshness FreshnessOn(this PantryItem item, DateOnly today, int expiringSoonDays) => item.DaysLeft(today) switch
    {
        null => Freshness.Unknown,
        < 0 => Freshness.Expired,
        var days when days <= expiringSoonDays => Freshness.ExpiringSoon,
        _ => Freshness.Fresh,
    };
}
