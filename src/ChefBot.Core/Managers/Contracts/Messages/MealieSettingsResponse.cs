namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How ChefBot reaches Mealie. The token itself is never included.</summary>
/// <param name="Url">Mealie's address. Empty when Mealie isn't used.</param>
/// <param name="HasApiToken">True when an API token is saved.</param>
/// <param name="IsConfigured">True when importing from Mealie is possible.</param>
public sealed record MealieSettingsResponse(string Url, bool HasApiToken, bool IsConfigured);
