namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Changes the advanced planning and import settings. A null value is set back to its default.</summary>
/// <param name="AvoidRepeatDays">Avoid repeating a recipe planned within this many days, from 0 to 365.</param>
/// <param name="ExpiringSoonDays">Pantry food expiring within this many days is planned first, from 0 to 30.</param>
/// <param name="PerishableWithinDays">Ingredients that keep no longer than this are worth sharing between meals, from 1 to 365.</param>
/// <param name="Words">The words that sort imported recipes into courses. Null sets every list back to its default.</param>
public sealed record AdvancedSettingsRequest(int? AvoidRepeatDays, int? ExpiringSoonDays, int? PerishableWithinDays, CourseWords? Words);
