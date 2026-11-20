namespace ChefBot.Core.Contracts.Messages;

/// <summary>Event: the sign-in rules changed, including whether HTTPS is required.</summary>
public sealed record SecuritySettingsChanged(bool RequireHttps);
