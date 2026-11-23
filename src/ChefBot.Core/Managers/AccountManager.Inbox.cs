using ChefBot.Core.Engines;
using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.Managers;

/// <content>Each person's inbox, the messages behind the bell on the web page.</content>
internal sealed partial class AccountManager
{
    /// <summary>How many messages each person keeps. Older ones are dropped as new ones arrive.</summary>
    internal const int InboxSize = 50;

    /// <summary>The <see cref="PlanStaged.Trigger"/> for a plan the weekly schedule staged.</summary>
    private const string ScheduleTrigger = "schedule";

    public async Task<InboxResponse> GetInboxAsync(Guid userId, CancellationToken cancellationToken) =>
        Describe(await inbox.ListAsync(userId, cancellationToken));

    public async Task<InboxResponse> AddInboxNoteAsync(Guid userId, InboxNoteRequest request, CancellationToken cancellationToken)
    {
        var text = ValidationEngine.InboxNote(request.Text);
        await validation.UserAsync(userId, cancellationToken);
        await AddAsync(userId, text, InboxSource.Page, sourceId: null, cancellationToken);
        await messages.PublishAsync(new InboxChanged(userId));
        await messages.CommitAsync(cancellationToken);
        return await GetInboxAsync(userId, cancellationToken);
    }

    public async Task<InboxResponse> MarkInboxReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        await inbox.MarkReadAsync(userId, clock.GetUtcNow(), cancellationToken);
        await messages.PublishAsync(new InboxChanged(userId));
        await messages.CommitAsync(cancellationToken);
        return await GetInboxAsync(userId, cancellationToken);
    }

    public async Task<InboxResponse> ClearInboxAsync(Guid userId, CancellationToken cancellationToken)
    {
        await inbox.ClearAsync(userId, cancellationToken);
        await messages.PublishAsync(new InboxChanged(userId));
        await messages.CommitAsync(cancellationToken);
        return new InboxResponse([], 0);
    }

    public async Task AnnouncePlanStagedAsync(PlanStaged message, Guid messageId, CancellationToken cancellationToken)
    {
        if (message.Trigger != ScheduleTrigger)
        {
            return;
        }

        var added = false;
        foreach (var user in await accounts.ListUsersAsync(cancellationToken))
        {
            if (await inbox.HasSourceAsync(user.Id, messageId, cancellationToken))
            {
                continue;
            }

            await AddAsync(user.Id, "The weekly schedule made a new plan. Review it on the Plan tab, then confirm it.", InboxSource.Plan, messageId, cancellationToken);
            added = true;
        }

        if (added)
        {
            await messages.PublishAsync(new InboxChanged(null));
            await messages.CommitAsync(cancellationToken);
        }
    }

    private static InboxResponse Describe(List<InboxEntry> entries) => new(
        entries.Select(entry => new InboxEntryResponse(entry.Id, entry.Text, entry.Source, entry.CreatedAt, entry.ReadAt is not null)).ToList(),
        entries.Count(entry => entry.ReadAt is null));

    /// <summary>Adds an unread message, then drops the oldest past <see cref="InboxSize"/>.</summary>
    private async Task AddAsync(Guid userId, string text, InboxSource source, Guid? sourceId, CancellationToken cancellationToken)
    {
        await inbox.AddAsync(
            new InboxEntry { UserId = userId, Text = text, Source = source, SourceId = sourceId, CreatedAt = clock.GetUtcNow() },
            cancellationToken);
        var dropped = (await inbox.ListAsync(userId, cancellationToken)).Skip(InboxSize).Select(entry => entry.Id).ToList();
        if (dropped.Count > 0)
        {
            await inbox.RemoveAsync(userId, dropped, cancellationToken);
        }
    }
}
