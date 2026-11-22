namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// One moment on the food history timeline. The web page chooses the wording; this says what happened.
/// </summary>
/// <param name="At">
/// When it happened, in the household's time zone: the exact time, or midnight when only the day is known. For a
/// <see cref="HistoryMomentKind.WeekOfPlates"/>, the last dish of the week.
/// </param>
/// <param name="Kind">What happened, which also says what <paramref name="Count"/> means.</param>
/// <param name="Title">The recipe, dish or person it is about. Null when it is about several.</param>
/// <param name="RecipeId">The recipe, when it is still in the library.</param>
/// <param name="Count">A number that goes with the kind: cooks, days, dinners, recipes or things bought.</param>
/// <param name="Dishes">The dishes of a week of plates or a recipe stampede, in order; empty for the rest.</param>
/// <param name="FavouredBy">For a cook, whose favourite the recipe was when it was cooked, oldest favourite first; empty for the rest.</param>
/// <param name="Review">For <see cref="HistoryMomentKind.Reviewed"/>, the review; null for the rest.</param>
public sealed record HistoryMoment(
    DateTimeOffset At,
    HistoryMomentKind Kind,
    string? Title,
    Guid? RecipeId,
    int? Count,
    IReadOnlyList<HistoryDish> Dishes,
    IReadOnlyList<string> FavouredBy,
    HistoryReview? Review = null);
