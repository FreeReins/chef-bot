namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Says the signed-in person has been shown the new achievements up to one of them.</summary>
/// <param name="UpTo">The newest one shown, from <see cref="AchievementsResponse.Unseen"/>.</param>
public sealed record AchievementsSeenRequest(Guid UpTo);
