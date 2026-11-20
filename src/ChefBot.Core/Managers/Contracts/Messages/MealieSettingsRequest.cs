namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>How to reach Mealie.</summary>
/// <param name="Url">Mealie's address, for example http://192.168.1.20:9000. Empty stops using Mealie.</param>
/// <param name="ApiToken">A new API token. Empty or null keeps the saved one.</param>
public sealed record MealieSettingsRequest(string? Url, string? ApiToken = null);
