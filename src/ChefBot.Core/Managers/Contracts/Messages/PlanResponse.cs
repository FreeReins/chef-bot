namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A weekly plan with its meals and shopping list.</summary>
/// <param name="Id">The plan's id.</param>
/// <param name="WeekStart">The first day of the planned week.</param>
/// <param name="WeekEnd">The last day of the planned week.</param>
/// <param name="Status">Staged while it is a proposal to review; Confirmed once the household shops and cooks from it.</param>
/// <param name="Meals">The planned meals, snacks and sweets.</param>
/// <param name="Shopping">What to buy for the plan, and where.</param>
/// <param name="StyleName">The plan style it was made with.</param>
/// <param name="Notes">Things worth knowing about the change that produced this response.</param>
public sealed record PlanResponse(
    Guid Id,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    PlanStatus Status,
    IReadOnlyList<MealResponse> Meals,
    IReadOnlyList<ShoppingItemResponse> Shopping,
    string StyleName,
    IReadOnlyList<string> Notes);
