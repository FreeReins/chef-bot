using Wolverine.Attributes;

namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>Event: the sign-in rules changed, including whether HTTPS is required.</summary>
[MessageIdentity("chefbot.security-settings-changed.v1")]
public sealed record SecuritySettingsChanged(bool RequireHttps);
