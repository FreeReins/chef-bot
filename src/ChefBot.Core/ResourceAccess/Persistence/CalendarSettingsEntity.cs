namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class CalendarSettingsEntity
{
    public int Id { get; set; } = 1;

    public string BreakfastTime { get; set; } = string.Empty;

    public string LunchTime { get; set; } = string.Empty;

    public string DinnerTime { get; set; } = string.Empty;

    public int MealMinutes { get; set; }

    public int SlowCookerHours { get; set; }
}
