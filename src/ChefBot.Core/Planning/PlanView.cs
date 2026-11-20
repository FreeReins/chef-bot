namespace ChefBot.Core.Planning;

/// <summary>A plan as returned to the caller, with notes about the change that was just made.</summary>
public sealed record PlanView(MealPlan Plan, IReadOnlyList<string> Notes);
