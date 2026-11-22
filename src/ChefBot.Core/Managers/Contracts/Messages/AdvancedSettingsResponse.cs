namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The advanced planning and import settings, with the defaults they can be set back to.</summary>
/// <param name="Current">The values in use.</param>
/// <param name="Defaults">ChefBot's own values, which suit most homes.</param>
/// <param name="IsSaved">False until these are first saved: until then they are the defaults.</param>
public sealed record AdvancedSettingsResponse(AdvancedSettingsValues Current, AdvancedSettingsValues Defaults, bool IsSaved);
