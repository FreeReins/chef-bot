namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// How many people plans are for, when next week is planned by itself, and how long leftovers keep. Anyone signed in
/// can change them.
/// </summary>
internal sealed class PlannerSettings
{
    /// <summary>The people a plan is for until the household says otherwise.</summary>
    public const int DefaultPeople = 2;

    public const int DefaultMaxLeftoverDays = 3;

    /// <summary>People eating each breakfast, lunch and dinner.</summary>
    public int People { get; set; } = DefaultPeople;

    /// <summary>True to stage next week's plan by itself each week.</summary>
    public bool ScheduleEnabled { get; set; } = true;

    public DayOfWeek ScheduleDay { get; set; } = DayOfWeek.Saturday;

    /// <summary>Local time of day, as HH:mm.</summary>
    public string ScheduleTime { get; set; } = "09:00";

    /// <summary>A lunch only uses a dinner cooked within this many days.</summary>
    public int MaxLeftoverDays { get; set; } = DefaultMaxLeftoverDays;

    /// <summary>False until these are first saved: until then they are the defaults. Not stored itself.</summary>
    public bool IsSaved { get; set; }
}
