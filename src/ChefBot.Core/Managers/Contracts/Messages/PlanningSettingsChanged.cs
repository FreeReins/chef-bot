using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the advanced planning settings changed, so the open plans' shopping lists may be out of date.</summary>
[MessageIdentity("chefbot.planning-settings-changed.v1")]
public sealed record PlanningSettingsChanged;
