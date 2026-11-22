namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The review a <see cref="HistoryMomentKind.Reviewed"/> moment tells.</summary>
/// <param name="Stars">From 1 to 5.</param>
/// <param name="Notes">What the reviewer had to say, or null when they only rated it.</param>
/// <param name="ReviewedBy">The reviewer's name when they wrote it.</param>
public sealed record HistoryReview(int Stars, string? Notes, string ReviewedBy);
