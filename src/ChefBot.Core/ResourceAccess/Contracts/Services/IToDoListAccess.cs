using ChefBot.Core.ResourceAccess.Contracts.Messages;

namespace ChefBot.Core.ResourceAccess.Contracts.Services;

/// <summary>To-do apps outside ChefBot: Home Assistant, Todoist and webhooks, which shopping items are sent to.</summary>
internal interface IToDoListAccess
{
    /// <summary>
    /// Adds the items to the target's list, in order. Stops at the first item the app refuses, or when it can't be
    /// reached, and says why. Never throws for the app's own failures.
    /// </summary>
    Task<ToDoDelivery> SendAsync(ShoppingTarget target, IReadOnlyList<ToDoItem> items, CancellationToken cancellationToken);
}
