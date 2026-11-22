using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the grocery stores, or where something is bought, changed, so the open plans' shopping lists may be out of date.</summary>
[MessageIdentity("chefbot.grocery-stores-changed.v1")]
public sealed record GroceryStoresChanged;
