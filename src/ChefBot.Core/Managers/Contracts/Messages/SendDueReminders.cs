using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command, scheduled for the daily reminder time: queue today's reminders if they haven't gone out yet, then
/// schedule the next check. Only the check saved as due is acted on, so a settings change or a restart that schedules
/// a new one leaves the old one to do nothing.
/// </summary>
/// <param name="DueAt">When it was scheduled for; null to look now and schedule afresh, as at startup.</param>
[MessageIdentity("chefbot.send-due-reminders.v1")]
public sealed record SendDueReminders(DateTimeOffset? DueAt = null);
