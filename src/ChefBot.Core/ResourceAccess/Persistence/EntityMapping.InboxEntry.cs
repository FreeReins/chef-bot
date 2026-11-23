using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Persistence;

/// <content>Copies InboxEntryEntity to and from InboxEntry.</content>
internal static partial class EntityMapping
{
    public static InboxEntry ToContract(this InboxEntryEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        Text = entity.Text,
        Source = entity.Source,
        SourceId = entity.SourceId,
        CreatedAt = entity.CreatedAt,
        ReadAt = entity.ReadAt,
    };

    public static InboxEntryEntity ToEntity(this InboxEntry entry) => new()
    {
        Id = entry.Id,
        UserId = entry.UserId,
        Text = entry.Text,
        Source = entry.Source,
        SourceId = entry.SourceId,
        CreatedAt = entry.CreatedAt,
        ReadAt = entry.ReadAt,
    };
}
