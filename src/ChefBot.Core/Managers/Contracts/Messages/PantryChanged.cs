using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the pantry was edited by hand, so the open plans' shopping lists may be out of date.</summary>
[MessageIdentity("chefbot.pantry-changed.v1")]
public sealed record PantryChanged;
