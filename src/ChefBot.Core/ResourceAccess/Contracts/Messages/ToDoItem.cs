namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>One thing to buy as a to-do app shows it.</summary>
/// <param name="Title">The item itself: "fish fillets, 600 g".</param>
/// <param name="Description">Where to buy it and what it is for, when known: "Corner Market · For Slaw Fish Tacos".</param>
internal sealed record ToDoItem(string Title, string? Description = null);
