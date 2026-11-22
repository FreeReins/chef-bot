using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command, scheduled for the daily backup time: make today's backup if it hasn't been made yet, then schedule the
/// next check. Only the check saved as due is acted on, so a settings change or a restart that schedules a new one
/// leaves the old one to do nothing.
/// </summary>
/// <param name="DueAt">When it was scheduled for; null to look now and schedule afresh, as at startup.</param>
[MessageIdentity("chefbot.run-due-backup.v1")]
public sealed record RunDueBackup(DateTimeOffset? DueAt = null);
