using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command, scheduled for when the weekly schedule is next due: stage next week's plan if its time has come, then
/// schedule the next check. Only the check saved as due is acted on, so a settings change or a restart that schedules
/// a new one leaves the old one to do nothing.
/// </summary>
/// <param name="DueAt">When it was scheduled for; null to look now and schedule afresh, as at startup.</param>
[MessageIdentity("chefbot.check-weekly-schedule.v1")]
public sealed record CheckWeeklySchedule(DateTimeOffset? DueAt = null);
