using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Engines.Contracts.Messages;

/// <summary>A proposed week, plus anything the household should know about how it was put together.</summary>
internal sealed record PlanDraft(MealPlan Plan, IReadOnlyList<string> Warnings);
