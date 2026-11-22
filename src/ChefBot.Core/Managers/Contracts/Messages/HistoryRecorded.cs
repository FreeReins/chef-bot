using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the food history gained or lost a moment, so what is worked out from it may have changed.</summary>
[MessageIdentity("chefbot.history-recorded.v1")]
public sealed record HistoryRecorded;
