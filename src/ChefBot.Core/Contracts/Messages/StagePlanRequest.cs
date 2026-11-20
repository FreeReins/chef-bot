namespace ChefBot.Core.Contracts.Messages;

/// <summary>Stage a plan. Send <c>{}</c> for the upcoming week.</summary>
/// <param name="WeekStart">The Sunday to plan from. Defaults to the upcoming week.</param>
/// <param name="Replace">Start a week over even though it was already confirmed.</param>
public sealed record StagePlanRequest(DateOnly? WeekStart = null, bool Replace = false);
