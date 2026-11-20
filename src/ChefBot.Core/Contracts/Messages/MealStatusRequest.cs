namespace ChefBot.Core.Contracts.Messages;

/// <summary>Mark a meal as planned, cooked or skipped.</summary>
public sealed record MealStatusRequest(MealStatus Status);
