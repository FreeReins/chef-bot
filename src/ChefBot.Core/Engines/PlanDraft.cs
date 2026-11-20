using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Engines;

/// <summary>A proposed week, plus anything the household should know about how it was put together.</summary>
internal sealed record PlanDraft(MealPlan Plan, IReadOnlyList<string> Warnings);
