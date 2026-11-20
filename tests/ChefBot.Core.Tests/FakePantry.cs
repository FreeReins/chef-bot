using ChefBot.Core.ResourceAccess;
using ChefBot.Core.ResourceAccess.Contracts;

namespace ChefBot.Core.Tests;

internal sealed class FakePantry : IPantryAccess
{
    public List<PantryItem> Rows { get; } = [];

    public Task<List<PantryItem>> ListAsync(CancellationToken cancellationToken) => Task.FromResult(Rows.Select(Copies.Of).ToList());

    public Task<PantryItem?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rows.FirstOrDefault(i => i.Id == id) is { } row ? Copies.Of(row) : null);

    public Task SaveAsync(PantryItem item, CancellationToken cancellationToken)
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
