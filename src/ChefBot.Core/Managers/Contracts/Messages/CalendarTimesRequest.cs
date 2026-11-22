namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>When meals appear in the calendar feed.</summary>
/// <param name="BreakfastTime">When breakfast starts, as local HH:mm.</param>
/// <param name="LunchTime">When lunch starts, as local HH:mm.</param>
/// <param name="DinnerTime">When dinner starts, as local HH:mm.</param>
/// <param name="MealMinutes">How long each meal's event lasts, from 5 to 240 minutes.</param>
/// <param name="SlowCookerHours">How many hours before a slow cooker dinner its reminder goes off, from 0 to 12. Zero for none.</param>
public sealed record CalendarTimesRequest(string? BreakfastTime, string? LunchTime, string? DinnerTime, int MealMinutes, int SlowCookerHours);
