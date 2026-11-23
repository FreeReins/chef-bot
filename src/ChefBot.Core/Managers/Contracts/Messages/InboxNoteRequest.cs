namespace ChefBot.Core.Managers.Contracts.Messages;

/// <summary>A note for the signed-in person's own inbox, such as "Recipe saved.", from the web page.</summary>
/// <param name="Text">What it says, as a plain sentence.</param>
public sealed record InboxNoteRequest(string Text);
