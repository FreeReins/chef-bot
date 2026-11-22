using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Command, at startup: work out the achievements once, so what was reached before ChefBot kept track is recorded
/// quietly, and anything reached from then on is announced.
/// </summary>
[MessageIdentity("chefbot.check-achievements.v1")]
public sealed record CheckAchievements;
