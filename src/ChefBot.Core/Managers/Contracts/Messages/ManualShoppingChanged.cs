using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the shopping added by hand changed: added, removed, bought or moved to another store.</summary>
[MessageIdentity("chefbot.manual-shopping-changed.v1")]
public sealed record ManualShoppingChanged;
