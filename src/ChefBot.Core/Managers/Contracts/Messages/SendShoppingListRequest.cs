namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>
/// Sends what is left to buy to a to-do app: the plan's items not yet bought, if a plan is given, and everything added
/// to the list by hand.
/// </summary>
/// <param name="TargetId">The to-do app to send to.</param>
/// <param name="PlanId">The plan whose shopping list goes too, or null for only the hand-added items.</param>
public sealed record SendShoppingListRequest(Guid TargetId, Guid? PlanId = null);
