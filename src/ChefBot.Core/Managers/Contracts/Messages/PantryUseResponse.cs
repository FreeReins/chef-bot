namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A pantry item, and which meals on the plan are going to use it.</summary>
/// <param name="Item">The pantry item.</param>
/// <param name="UsedByPlan">False when nothing on the plan uses it, which makes it a candidate for a swap.</param>
/// <param name="UsedFor">The meals still to be cooked that use it, soonest first. Empty when <paramref name="UsedByPlan"/> is false.</param>
public sealed record PantryUseResponse(PantryItemResponse Item, bool UsedByPlan, IReadOnlyList<PantryMealUse> UsedFor);
