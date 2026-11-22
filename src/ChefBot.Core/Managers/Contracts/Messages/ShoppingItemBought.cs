using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: one thing on a plan's shopping list was bought and put in the pantry.</summary>
/// <param name="PlanId">The plan.</param>
/// <param name="ItemId">The shopping item.</param>
[MessageIdentity("chefbot.shopping-item-bought.v1")]
public sealed record ShoppingItemBought(Guid PlanId, Guid ItemId);
