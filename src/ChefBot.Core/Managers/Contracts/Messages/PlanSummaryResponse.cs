namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A plan whose week hasn't ended, without its meals: enough to choose between this week and next.</summary>
/// <param name="Id">The plan's id.</param>
/// <param name="WeekStart">The first day of the planned week.</param>
/// <param name="WeekEnd">The last day of the planned week.</param>
/// <param name="Status">Staged while it is a draft to review; Confirmed once the household shops and cooks from it.</param>
/// <param name="StyleName">The plan style it was made with.</param>
public sealed record PlanSummaryResponse(Guid Id, DateOnly WeekStart, DateOnly WeekEnd, PlanStatus Status, string StyleName);
