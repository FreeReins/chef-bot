using System.Globalization;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>When meals appear in the calendar feed. Times are local, as HH:mm. Admins change them.</summary>
internal sealed class CalendarSettings
{
    public string BreakfastTime { get; set; } = "08:00";

    public string LunchTime { get; set; } = "12:30";

    public string DinnerTime { get; set; } = "18:30";

    /// <summary>How long each meal's event lasts.</summary>
    public int MealMinutes { get; set; } = 45;

    /// <summary>How long before a slow cooker dinner its reminder to start the slow cooker goes off. Zero for none.</summary>
    public int SlowCookerHours { get; set; } = 8;

    /// <summary>False until these are first saved: until then they are the defaults. Not stored itself.</summary>
    public bool IsSaved { get; set; }

    /// <summary>The time of day in <paramref name="text"/>, or <paramref name="fallback"/> when it isn't a valid HH:mm.</summary>
    public static TimeOnly Time(string? text, TimeOnly fallback) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : fallback;
}
