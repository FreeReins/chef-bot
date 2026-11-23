namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>One message in the signed-in person's inbox.</summary>
/// <param name="Id">The message.</param>
/// <param name="Text">What it says, as a plain sentence.</param>
/// <param name="Source">What put it there.</param>
/// <param name="CreatedAt">When it arrived, in UTC.</param>
/// <param name="Read">Whether the inbox has been opened since it arrived.</param>
public sealed record InboxEntryResponse(Guid Id, string Text, InboxSource Source, DateTimeOffset CreatedAt, bool Read);
