namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>The signed-in person's inbox: the messages behind the bell on the web page.</summary>
/// <param name="Entries">The newest messages, newest first. Older ones are dropped.</param>
/// <param name="Unread">How many of them haven't been seen yet.</param>
public sealed record InboxResponse(IReadOnlyList<InboxEntryResponse> Entries, int Unread);
