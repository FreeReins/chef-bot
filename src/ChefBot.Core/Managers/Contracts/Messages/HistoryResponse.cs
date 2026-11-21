namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The household's food history: what was cooked, added and celebrated, one chapter per month.</summary>
/// <param name="Chapters">Months with something in them, newest first.</param>
/// <param name="OlderBefore">The <see cref="HistoryRequest.Before"/> that asks for the next, older chapters; null when these are the oldest.</param>
/// <param name="Since">The day of the first moment ever kept; null when there is none yet.</param>
/// <param name="TotalDinners">Every dinner cooked since then.</param>
public sealed record HistoryResponse(IReadOnlyList<HistoryChapter> Chapters, DateOnly? OlderBefore, DateOnly? Since, int TotalDinners);
