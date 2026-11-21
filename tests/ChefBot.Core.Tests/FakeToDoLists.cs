using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

/// <summary>To-do apps that keep what they are sent. Set <see cref="TakeOnly"/> to have them refuse items after that many.</summary>
internal sealed class FakeToDoLists : IToDoListAccess
{
    public List<(ShoppingTarget Target, ToDoItem Item)> Sent { get; } = [];

    public int? TakeOnly { get; set; }

    public Task<ToDoDelivery> SendAsync(ShoppingTarget target, IReadOnlyList<ToDoItem> items, CancellationToken cancellationToken)
    {
        var taken = Math.Min(items.Count, TakeOnly ?? items.Count);
        Sent.AddRange(items.Take(taken).Select(item => (target, item)));
        return Task.FromResult(taken == items.Count ? new ToDoDelivery(taken) : new ToDoDelivery(taken, $"{target.Name} answered HTTP 500."));
    }
}
