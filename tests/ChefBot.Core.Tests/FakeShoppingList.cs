using ChefBot.Core.ResourceAccess.Contracts.Messages;
using ChefBot.Core.ResourceAccess.Contracts.Services;

namespace ChefBot.Core.Tests;

internal sealed class FakeShoppingList : IShoppingListAccess
{
    public List<ManualShoppingItem> Rows { get; } = [];

    public Task<List<ManualShoppingItem>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task<ManualShoppingItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(i => i.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(ManualShoppingItem item, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(i => i.Id == item.Id);
        Rows.Add(Copies.Of(item));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        Rows.RemoveAll(i => i.Id == id);
        return Task.CompletedTask;
    }
}
