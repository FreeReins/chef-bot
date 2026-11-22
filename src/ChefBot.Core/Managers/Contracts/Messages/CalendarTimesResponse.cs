namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>When meals appear in the calendar feed.</summary>
/// <param name="BreakfastTime">When breakfast starts, as local HH:mm.</param>
/// <param name="LunchTime">When lunch starts, as local HH:mm.</param>
/// <param name="DinnerTime">When dinner starts, as local HH:mm.</param>
/// <param name="MealMinutes">How long each meal's event lasts.</param>
/// <param name="SlowCookerHours">How many hours before a slow cooker dinner its reminder goes off. Zero for none.</param>
/// <param name="IsSaved">False until these are first saved: until then they are the defaults.</param>
public sealed record CalendarTimesResponse(
    string BreakfastTime, string LunchTime, string DinnerTime, int MealMinutes, int SlowCookerHours, bool IsSaved);
