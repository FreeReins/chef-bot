using ChefBot.Core.Utilities;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>How to reach Mealie. Empty means ChefBot runs without Mealie.</summary>
internal sealed class MealieSettings
{
    /// <summary>The base address, for example http://192.168.1.20:9000.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>An API token created in the Mealie user profile. Never sent back to the page.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public bool IsConfigured => WebAddress.IsHttp(Url) && ApiToken.Length > 0;
}
