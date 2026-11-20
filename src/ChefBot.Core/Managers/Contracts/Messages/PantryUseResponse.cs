namespace ChefBot.Core.Managers.Contracts.Messages;

/// <param name="Item">The pantry item.</param>
/// <param name="UsedByPlan">False when nothing on the plan uses it, which makes it a candidate for a swap.</param>
public sealed record PantryUseResponse(PantryItemResponse Item, bool UsedByPlan);
