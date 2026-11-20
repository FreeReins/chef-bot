namespace ChefBot.Infrastructure.Mealie;

/// <summary>How to reach a Mealie instance. Optional: leave empty to run without Mealie.</summary>
public sealed class MealieOptions
{
    /// <summary>The base address, for example http://192.168.1.20:9000.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>An API token created in the Mealie user profile.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public bool IsConfigured =>
        Uri.TryCreate(Url, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(ApiToken);
}
