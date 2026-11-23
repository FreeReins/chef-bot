using ChefBot.Core.Managers.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

internal sealed class InboxEntryEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public InboxSource Source { get; set; }

    public Guid? SourceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}
