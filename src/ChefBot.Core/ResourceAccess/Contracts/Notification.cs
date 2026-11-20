namespace ChefBot.Core.ResourceAccess.Contracts;

/// <summary>Something worth telling the household.</summary>
/// <param name="Type">One of <see cref="NotificationTypes"/>.</param>
/// <param name="Title">A short heading.</param>
/// <param name="Message">A ready-to-show sentence or two.</param>
/// <param name="Data">The facts behind the message, so the receiver can word its own.</param>
internal sealed record Notification(string Type, string Title, string Message, IReadOnlyDictionary<string, object?> Data);
