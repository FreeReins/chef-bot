namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How plans weigh repeats, expiring food and perishables, and the words that sort imported recipes.</summary>
/// <param name="AvoidRepeatDays">Avoid repeating a recipe planned within this many days.</param>
/// <param name="ExpiringSoonDays">Pantry food expiring within this many days is planned first.</param>
/// <param name="PerishableWithinDays">Ingredients that keep no longer than this are worth sharing between meals.</param>
/// <param name="Words">The words that sort imported recipes into courses.</param>
public sealed record AdvancedSettingsValues(int AvoidRepeatDays, int ExpiringSoonDays, int PerishableWithinDays, CourseWords Words);
