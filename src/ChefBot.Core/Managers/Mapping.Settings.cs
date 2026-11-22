using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>The planning, calendar and advanced settings as responses.</content>
internal static partial class Mapping
{
    public static PlannerSettingsResponse ToResponse(this PlannerSettings settings, DateTimeOffset? nextStaging) => new(
        settings.People,
        settings.ScheduleEnabled,
        settings.ScheduleDay,
        settings.ScheduleTime,
        settings.MaxLeftoverDays,
        nextStaging,
        settings.IsSaved);

    public static CalendarTimesResponse ToResponse(this CalendarSettings times) => new(
        times.BreakfastTime, times.LunchTime, times.DinnerTime, times.MealMinutes, times.SlowCookerHours, times.IsSaved);

    public static AdvancedSettingsResponse ToResponse(this AdvancedSettings settings) =>
        new(settings.ToValues(), new AdvancedSettings().ToValues(), settings.IsSaved);

    private static AdvancedSettingsValues ToValues(this AdvancedSettings settings) => new(
        settings.AvoidRepeatDays,
        settings.ExpiringSoonDays,
        settings.PerishableWithinDays,
        new CourseWords(
            [.. settings.Words.BreakfastWords],
            [.. settings.Words.SnackWords],
            [.. settings.Words.SweetWords],
            [.. settings.Words.DrinkWords],
            [.. settings.Words.SlowCookerWords],
            [.. settings.Words.NotAMealWords],
            [.. settings.Words.MainWords]));
}
