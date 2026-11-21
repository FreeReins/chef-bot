namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One month of the food history, with what the page needs to give it a title.</summary>
/// <param name="Month">The first day of the month.</param>
/// <param name="Moments">What happened, newest first.</param>
/// <param name="Dinners">How many dinners were cooked that month.</param>
/// <param name="TopDish">The dish cooked most that month, when one was cooked more than once.</param>
/// <param name="TopDishCooks">How many times <paramref name="TopDish"/> was cooked; zero when there is none.</param>
/// <param name="TopTag">The recipe tag cooked most that month, when one came up more than once.</param>
public sealed record HistoryChapter(
    DateOnly Month, IReadOnlyList<HistoryMoment> Moments, int Dinners, string? TopDish, int TopDishCooks, string? TopTag);
