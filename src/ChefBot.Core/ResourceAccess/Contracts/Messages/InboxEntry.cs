using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One message in a person's inbox, the list behind the bell on the web page.</summary>
internal sealed class InboxEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>The person it is for.</summary>
    public Guid UserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public InboxSource Source { get; set; }

    /// <summary>The message that added it, so handling the same message again adds nothing. Null for the page's notes.</summary>
    public Guid? SourceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the person opened the inbox with this in it. Null while unread.</summary>
    public DateTimeOffset? ReadAt { get; set; }
}
