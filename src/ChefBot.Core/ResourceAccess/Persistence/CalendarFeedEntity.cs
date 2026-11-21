namespace ChefBot.Core.ResourceAccess.Persistence;

// Settings: one row, with Id 1.
internal sealed class CalendarFeedEntity
{
    public int Id { get; set; } = 1;

    public string Token { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAt { get; set; }
}
