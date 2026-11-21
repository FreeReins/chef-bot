namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Stage a plan. Send <c>{}</c> for the upcoming week in the default plan style.</summary>
/// <param name="WeekStart">The Sunday to plan from. Defaults to the upcoming week.</param>
/// <param name="Replace">Start a week over even though it was already confirmed.</param>
/// <param name="StyleId">The plan style to use. Defaults to the household's default style.</param>
/// <param name="Keep">Meals of the week's current plan to keep as they are when planning it again, as when shuffling the rest.</param>
public sealed record StagePlanRequest(DateOnly? WeekStart = null, bool Replace = false, Guid? StyleId = null, IReadOnlyList<Guid>? Keep = null);
