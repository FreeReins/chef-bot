namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>How many items a to-do app took, and what went wrong when it didn't take them all.</summary>
internal sealed record ToDoDelivery(int Sent, string? Problem = null);
