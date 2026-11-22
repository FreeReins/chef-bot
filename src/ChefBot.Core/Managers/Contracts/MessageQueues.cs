namespace ChefBot.Core.Managers.Contracts;

/// <summary>
/// The durable local queues the message handlers run on, set on each handler with
/// <c>[StickyHandler(MessageQueues.…)]</c> and configured in <see cref="CoreRegistration"/>. A queue groups the
/// handlers that must not run at the same time as each other, or that call the same outside service.
/// </summary>
public static class MessageQueues
{
    /// <summary>Keeps the open plans in step: one message at a time, next to the plan writes made by requests.</summary>
    public const string Plans = "plans";

    /// <summary>Writes the food history from the events that make it: one message at a time.</summary>
    public const string History = "history";

    /// <summary>Works out newly earned achievements: one message at a time, so a tier is never saved twice.</summary>
    public const string Achievements = "achievements";

    /// <summary>Downloads recipe photos from the internet, a couple at a time.</summary>
    public const string Images = "images";

    /// <summary>Delivers notifications to the webhook, a couple at a time.</summary>
    public const string Notifications = "notifications";

    /// <summary>The scheduled jobs, backups and settings that apply to the running server: one at a time.</summary>
    public const string Maintenance = "maintenance";

    /// <summary>
    /// Tells open pages what changed. Kept in memory only: a page that misses a nudge loads everything again when it
    /// reconnects, so these messages are not worth storing.
    /// </summary>
    public const string Live = "live";
}
