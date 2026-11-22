using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command: deliver one of a day's reminders to the webhook. Queued together with marking the day's reminders as
/// done, so each goes out once, and one webhook that is down is retried on its own without holding up the others.
/// </summary>
/// <param name="Type">Which reminder: "slow_cooker", "meals_today", "expiring_food" or "shopping".</param>
/// <param name="On">The day it is for. It is worded when delivered, and dropped if it no longer applies.</param>
[MessageIdentity("chefbot.send-reminder.v1")]
public sealed record SendReminder(string Type, DateOnly On);
