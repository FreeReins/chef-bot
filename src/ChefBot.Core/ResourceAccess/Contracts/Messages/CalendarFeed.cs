namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>
/// The household's calendar feed. Its token is the secret in the feed's address, which calendar apps fetch without
/// signing in. It is kept as it is, not hashed, so everyone signed in can copy the address again.
/// </summary>
internal sealed class CalendarFeed
{
    /// <summary>The secret in the address. Empty when the feed is off.</summary>
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAt { get; set; }
}
