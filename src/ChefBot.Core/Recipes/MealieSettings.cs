namespace ChefBot.Core.Recipes;

/// <summary>
/// How to reach Mealie. One stored row, changed on the Settings tab. Mealie__Url and Mealie__ApiToken give the
/// starting values until an admin saves something. Empty means ChefBot runs without Mealie.
/// </summary>
public sealed class MealieSettings
{
    public int Id { get; set; } = 1;

    /// <summary>The base address, for example http://192.168.1.20:9000.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>An API token created in the Mealie user profile. Never sent back to the page.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public bool IsConfigured => IsWebAddress(Url) && ApiToken.Length > 0;

    /// <summary>True for an absolute http or https address.</summary>
    public static bool IsWebAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
